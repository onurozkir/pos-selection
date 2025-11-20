using System.Data;

namespace PayTR.PosSelection.Infrastructure.Db
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}

