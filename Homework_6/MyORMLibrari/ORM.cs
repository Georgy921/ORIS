using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Npgsql;

namespace ORMLibrary
{
    public class ORM
    {
        private readonly string _connectionString;

        public ORM(string connectionString)
        {
            _connectionString = connectionString;
        }

        public T Create<T>(T entity, string tableName) where T : class
        {
            using (var dataSource = NpgsqlDataSource.Create(_connectionString))
            {
                var sqlQueryStringBuilder = new StringBuilder();
                sqlQueryStringBuilder.Append($"INSERT INTO {tableName.ToLower()} (");

                foreach (var prop in typeof(T).GetProperties().Skip(1))
                {
                    sqlQueryStringBuilder.Append($"{prop.Name.ToLower()},");
                }
                sqlQueryStringBuilder.Remove(sqlQueryStringBuilder.Length - 1, 1);
                sqlQueryStringBuilder.Append(") VALUES (");

                foreach (var prop in typeof(T).GetProperties().Skip(1))
                {
                    sqlQueryStringBuilder.Append($"@{prop.Name.ToLower()},");
                }
                sqlQueryStringBuilder.Remove(sqlQueryStringBuilder.Length - 1, 1);
                sqlQueryStringBuilder.Append(")");

                var command = dataSource.CreateCommand(sqlQueryStringBuilder.ToString());
                foreach (var prop in typeof(T).GetProperties().Skip(1))
                {
                    command.Parameters.AddWithValue($"{prop.Name.ToLower()}", prop.GetValue(entity) ?? DBNull.Value);
                }

                command.ExecuteNonQuery();

                return entity;
            }
        }

        public T ReadById<T>(int id) where T : class, new()
        {
            using (var dataSource = NpgsqlDataSource.Create(_connectionString))
            {
                string sqlQuery = $"SELECT * FROM {typeof(T).Name.ToLower()} WHERE id = @id";
                var command = dataSource.CreateCommand(sqlQuery);
                command.Parameters.AddWithValue("@id", id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return Map<T>(reader);
                    }
                }
            }
            return null;
        }

        public List<T> ReadByAll<T>() where T : class, new()
        {
            using (var dataSource = NpgsqlDataSource.Create(_connectionString))
            {
                string sqlQuery = $"SELECT * FROM {typeof(T).Name.ToLower()}";
                var command = dataSource.CreateCommand(sqlQuery);

                var results = new List<T>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(Map<T>(reader));
                    }
                }

                return results;
            }
        }

        public void Update<T>(int id, T entity, string tableName)
        {
            using (var dataSource = NpgsqlDataSource.Create(_connectionString))
            {
                var sqlQueryStringBuilder = new StringBuilder();
                sqlQueryStringBuilder.Append($"UPDATE {tableName.ToLower()} SET ");

                foreach (var prop in typeof(T).GetProperties().Skip(1))
                {
                    sqlQueryStringBuilder.Append($"{prop.Name.ToLower()} = @{prop.Name.ToLower()},");
                }

                sqlQueryStringBuilder.Remove(sqlQueryStringBuilder.Length - 1, 1);
                sqlQueryStringBuilder.Append(" WHERE id = @id");

                var command = dataSource.CreateCommand(sqlQueryStringBuilder.ToString());

                foreach (var prop in typeof(T).GetProperties().Skip(1))
                {
                    command.Parameters.AddWithValue($"{prop.Name.ToLower()}", prop.GetValue(entity) ?? DBNull.Value);
                }

                command.Parameters.AddWithValue("@id", id);

                command.ExecuteNonQuery();
            }
        }

        public void Delete(int id, string tableName)
        {
            using (var dataSource = NpgsqlDataSource.Create(_connectionString))
            {
                string sqlQuery = $"DELETE FROM {tableName.ToLower()} WHERE id = @id";
                var command = dataSource.CreateCommand(sqlQuery);
                command.Parameters.AddWithValue("@id", id);

                command.ExecuteNonQuery();
            }
        }


        public IEnumerable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class, new()
        {
            var sqlQuery = BuildSqlQuery(predicate, singleResult: false);
            return ExecuteQueryMultiple<T>(sqlQuery);
        }

        public T FirstOrDefault<T>(Expression<Func<T, bool>> predicate) where T : class, new()
        {
            var sqlQuery = BuildSqlQuery(predicate, singleResult: true);
            return ExecuteQuerySingle<T>(sqlQuery);
        }

        private string BuildSqlQuery<T>(Expression<Func<T, bool>> predicate, bool singleResult)
        {
            var tableName = typeof(T).Name.ToLower();
            var whereClause = ParseExpression(predicate.Body);
            var limitClause = singleResult ? "LIMIT 1" : string.Empty;

            return $"SELECT * FROM {tableName} WHERE {whereClause} {limitClause}".Trim();
        }

        private string ParseExpression(Expression expression)
        {
            if (expression is BinaryExpression binary)
            {
                var left = ParseExpression(binary.Left);
                var right = ParseExpression(binary.Right);
                var op = GetSqlOperator(binary.NodeType);
                return $"({left} {op} {right})";
            }
            else if (expression is MemberExpression member)
            {
                return member.Member.Name.ToLower();
            }
            else if (expression is ConstantExpression constant)
            {
                return FormatConstant(constant.Value);
            }

            throw new NotSupportedException($"Unsupported expression type: {expression.GetType().Name}");
        }

        private string GetSqlOperator(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.NotEqual => "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.LessThan => "<",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThanOrEqual => "<=",
                _ => throw new NotSupportedException($"Unsupported node type: {nodeType}")
            };
        }

        private string FormatConstant(object value)
        {
            return value is string ? $"'{value}'" : value.ToString();
        }

        private T Map<T>(NpgsqlDataReader reader) where T : class, new()
        {
            var obj = new T();

            foreach (var prop in typeof(T).GetProperties())
            {
                var data = reader[prop.Name];
                prop.SetValue(obj, Convert.ChangeType(data, prop.PropertyType), null);
            }

            return obj;
        }

        private T ExecuteQuerySingle<T>(string query) where T : class, new()
        {
            using (var dataSource = NpgsqlDataSource.Create(_connectionString))
            {
                var command = dataSource.CreateCommand(query);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return Map<T>(reader);
                    }
                }
            }
            return null;
        }

        private IEnumerable<T> ExecuteQueryMultiple<T>(string query) where T : class, new()
        {
            using (var dataSource = NpgsqlDataSource.Create(_connectionString))
            {
                var results = new List<T>();
                var command = dataSource.CreateCommand(query);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(Map<T>(reader));
                    }
                }
                return results;
            }
        }
    }
}