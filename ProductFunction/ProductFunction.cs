using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace ProductFunction
{
    public static class ProductFunction
    {
        private static readonly string connectionString = "ConnectionString";

        [FunctionName("CreateProduct")]
        public static async Task<IActionResult> CreateProduct(
                [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] HttpRequest req,
                ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string name = data?.name;
            string description = data?.description;
            decimal price = data?.price;
            int storeId = data?.storeId;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand("InsertProduct", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@Description", description);
                        command.Parameters.AddWithValue("@Price", price);
                        command.Parameters.AddWithValue("@StoreId", storeId);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (SqlException e)
            {

                return new BadRequestObjectResult(e.Message);
            }

            return new OkResult();
        }

        [FunctionName("GetProducts")]
        public static async Task<IActionResult> GetProducts(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequest req,
                ILogger log)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand("SELECT dbo.GetProductsAsJSON()", connection))
                {
                    string productsJson = (string)await command.ExecuteScalarAsync();
                    return new OkObjectResult(productsJson);
                }
            }
        }

        [FunctionName("GetProduct")]
        public static async Task<IActionResult> GetProduct(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{id}")] HttpRequest req,
        ILogger log, int id)
        {
            var product = await GetProductById(id);
            if (product == null)
                return new NotFoundObjectResult("Product not found.");

            return new OkObjectResult(product);
        }

        [FunctionName("UpdateProduct")]
        public static async Task<IActionResult> UpdateProduct(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "products/{id}")] HttpRequest req,
        ILogger log, int id)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string name = data?.name;
            string description = data?.description;
            decimal price = data?.price;
            int storeId = data?.storeId;

            var product = await GetProductById(id);
            if (product == null)
                return new NotFoundObjectResult("Product not found.");

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand("UpdateProduct", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@Description", description);
                        command.Parameters.AddWithValue("@Price", price);
                        command.Parameters.AddWithValue("@StoreId", storeId);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (SqlException e)
            {

                return new BadRequestObjectResult(e.Message);
            }

            return new NoContentResult();
        }

        [FunctionName("DeleteProduct")]
        public static async Task<IActionResult> DeleteProduct(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "products/{id}")] HttpRequest req,
        ILogger log, int id)
        {
            var product = await GetProductById(id);
            if (product == null)
                return new NotFoundObjectResult("Product not found");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand("DeleteProduct", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Id", id);

                    await command.ExecuteNonQueryAsync();
                }
            }

            return new OkResult();
        }

        private static async Task<Product>  GetProductById(int productId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand("GetProduct", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Id", productId);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            var product = new Product
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = reader.GetString(2),
                                Price = reader.GetDecimal(3),
                                StoreId = reader.GetInt32(4)
                            };
                            return product;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }
    }
}
