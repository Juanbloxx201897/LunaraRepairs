using System.Globalization;
using System.Reflection;
using Lunara.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lunara.Api.Data;

// Importa los datos CSV existentes al crear por primera vez la base de datos SQLite.
public sealed class LegacyCsvImporter(string directory)
{
    public async Task LoadIntoAsync(LunaraDbContext db)
    {
        db.Clients.AddRange(await ReadAsync<Client>("clients.csv"));
        db.Technicians.AddRange(await ReadAsync<Technician>("technicians.csv"));
        db.SpareParts.AddRange(await ReadAsync<SparePart>("spare-parts.csv"));
        db.SparePartPurchases.AddRange(await ReadAsync<SparePartPurchase>("spare-part-purchases.csv"));
        db.AuthUsers.AddRange(await ReadAsync<AuthUser>("auth-users.csv"));
        db.Devices.AddRange(await ReadAsync<Device>("devices.csv"));
        db.RepairOrders.AddRange(await ReadAsync<RepairOrder>("repairs.csv"));
        db.RepairStatusHistory.AddRange(await ReadAsync<RepairStatusHistory>("repair-history.csv"));
        await db.SaveChangesAsync();
    }

    private async Task<List<T>> ReadAsync<T>(string fileName) where T : class, new()
    {
        var path = Path.Combine(directory, fileName);
        if (!File.Exists(path))
            return [];

        var rows = Parse(await File.ReadAllTextAsync(path));
        if (rows.Count == 0)
            throw new InvalidDataException($"El archivo de datos está vacío: {path}");

        var properties = GetScalarProperties<T>();
        var headers = rows[0];
        var optionalHeaders = typeof(T) == typeof(RepairOrder)
            ? new HashSet<string>(["LaborCost", "BudgetPartsJson"], StringComparer.Ordinal)
            : [];
        var expectedHeaders = properties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        if (headers.Distinct(StringComparer.Ordinal).Count() != headers.Length ||
            headers.Any(header => !expectedHeaders.Contains(header)) ||
            expectedHeaders.Except(headers, StringComparer.Ordinal)
                .Any(header => !optionalHeaders.Contains(header)))
            throw new InvalidDataException($"Los encabezados no coinciden con el formato esperado: {path}");
        var propertyIndexes = headers.Select(header =>
        {
            var propertyIndex = Array.FindIndex(properties, property => property.Name == header);
            return (PropertyIndex: propertyIndex, Header: header);
        }).ToArray();

        var entities = new List<T>(rows.Count - 1);
        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (row.Length != headers.Length)
                throw new InvalidDataException($"La fila {rowIndex + 1} tiene una cantidad incorrecta de columnas: {path}");

            var entity = new T();
            for (var columnIndex = 0; columnIndex < propertyIndexes.Length; columnIndex++)
            {
                var (propertyIndex, header) = propertyIndexes[columnIndex];
                try
                {
                    properties[propertyIndex].SetValue(
                        entity,
                        ParseValue(row[columnIndex], properties[propertyIndex]));
                }
                catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentException)
                {
                    throw new InvalidDataException(
                        $"Valor no válido en la fila {rowIndex + 1}, columna {header}: {path}",
                        exception);
                }
            }

            entities.Add(entity);
        }

        return entities;
    }

    private static PropertyInfo[] GetScalarProperties<T>() =>
        typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property =>
                property.CanRead &&
                property.CanWrite &&
                property.GetIndexParameters().Length == 0 &&
                IsSupported(property.PropertyType))
            .OrderBy(property => property.MetadataToken)
            .ToArray();

    private static bool IsSupported(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(string) || type == typeof(int) ||
               type == typeof(decimal) || type == typeof(bool) ||
               type == typeof(DateTime);
    }

    private static object? ParseValue(string value, PropertyInfo property)
    {
        var propertyType = property.PropertyType;
        var targetType = Nullable.GetUnderlyingType(propertyType);
        if (value.Length == 0)
        {
            if (targetType is not null ||
                new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable)
                return null;
            if (propertyType == typeof(string))
                return "";
            throw new FormatException("El valor obligatorio está vacío.");
        }

        targetType ??= propertyType;
        if (targetType == typeof(string)) return value;
        if (targetType == typeof(int)) return int.Parse(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(decimal)) return decimal.Parse(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(bool)) return bool.Parse(value);
        if (targetType == typeof(DateTime))
            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        throw new InvalidOperationException($"Tipo CSV no compatible: {propertyType.Name}");
    }

    private static List<string[]> Parse(string content)
    {
        var rows = new List<string[]>();
        var row = new List<string>();
        var field = new System.Text.StringBuilder();
        var quoted = false;

        for (var index = 0; index < content.Length; index++)
        {
            var character = content[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < content.Length && content[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else if (character == '"')
                {
                    quoted = false;
                }
                else
                {
                    field.Append(character);
                }

                continue;
            }

            if (character == '"' && field.Length == 0)
                quoted = true;
            else if (character == ',')
                AddField();
            else if (character == '\n')
                AddRow();
            else if (character != '\r')
                field.Append(character);
        }

        if (quoted)
            throw new InvalidDataException("El CSV termina dentro de un campo entrecomillado.");
        if (field.Length > 0 || row.Count > 0)
            AddRow();

        return rows;

        void AddField()
        {
            row.Add(field.ToString());
            field.Clear();
        }

        void AddRow()
        {
            AddField();
            rows.Add(row.ToArray());
            row.Clear();
        }
    }
}
