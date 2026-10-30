using System.Data;
using Dapper;

namespace Abank.Infrastructure.Persistence;

/// <summary>
/// Dapper no sabe enviar <see cref="DateOnly"/> como parámetro. Este handler lo manda como
/// <c>date</c> y, al leer, acepta lo que entregue Npgsql para esa columna.
/// </summary>
internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value;
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly date => date,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        _ => throw new DataException($"No se puede convertir {value.GetType().Name} a DateOnly.")
    };
}
