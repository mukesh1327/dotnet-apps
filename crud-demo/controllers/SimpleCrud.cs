using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;

namespace WebApplication3.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string? _connectionString;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(IConfiguration configuration, ILogger<EmployeeController> logger)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // Employee Model
        public class EmployeeInput
        {
            [Required(ErrorMessage = "Employee ID is required")]
            [StringLength(50, ErrorMessage = "Employee ID cannot exceed 50 characters")]
            public required string EmployeeId { get; set; }

            [Required(ErrorMessage = "Employee Name is required")]
            [StringLength(100, ErrorMessage = "Employee Name cannot exceed 100 characters")]
            public required string EmployeeName { get; set; }

            [StringLength(50)]
            public string? Department { get; set; }

            [Range(18, 65, ErrorMessage = "Age must be between 18 and 65")]
            public int Age { get; set; }
        }

        // 🔹 GET: api/employee/GetConnectionString
        [HttpGet("GetConnectionString")]
        public IActionResult GetConnectionString()
        {
            _logger.LogInformation("Fetching connection string...");
            return Ok(new
            {
                connectionString = _connectionString
            });
        }

        // 🔹 CREATE: Insert Employee
        [HttpPost("InsertEmployee")]

        public IActionResult InsertEmployee([FromBody] EmployeeInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                string query = @"INSERT INTO EmployeeDetails (EmployeeId, EmployeeName, Department, Age)
                                 VALUES (@EmployeeId, @EmployeeName, @Department, @Age)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@EmployeeId", input.EmployeeId);
                command.Parameters.AddWithValue("@EmployeeName", input.EmployeeName);
                command.Parameters.AddWithValue("@Department", input.Department != null ? (object)input.Department : DBNull.Value);
                command.Parameters.AddWithValue("@Age", input.Age);

                int rows = command.ExecuteNonQuery();
                _logger.LogInformation($"Inserted Employee: {input.EmployeeId} - {input.EmployeeName}");

                return rows > 0
                    ? Ok(new { Message = "Employee inserted successfully" })
                    : BadRequest(new { Message = "Employee insertion failed" });
            }
            catch (SqlException ex) when (ex.Number == 2627)
            {
                return Conflict(new { Message = "Employee ID already exists" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting employee");
                return StatusCode(500, new { Message = "Internal Server Error", Error = ex.Message });
            }
        }

        // 🔹 READ: Get All Employees
        [HttpGet("GetAllEmployees")]
        

        public IActionResult GetAllEmployees()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                string query = "SELECT EmployeeId, EmployeeName, Department, Age FROM EmployeeDetails";
                using var command = new SqlCommand(query, connection);
                using var reader = command.ExecuteReader();

                var employees = new List<object>();
                while (reader.Read())
                {
                    employees.Add(new
                    {
                        EmployeeId = reader["EmployeeId"].ToString(),
                        EmployeeName = reader["EmployeeName"].ToString(),
                        Department = reader["Department"].ToString(),
                        Age = Convert.ToInt32(reader["Age"])
                    });
                }

                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employees");
                return StatusCode(500, new { Message = "Internal Server Error", Error = ex.Message });
            }
        }

        // 🔹 READ: Get Employee by ID
        [HttpGet("GetEmployeeById/{employeeId}")]
        

        public IActionResult GetEmployeeById(string employeeId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                string query = "SELECT EmployeeId, EmployeeName, Department, Age FROM EmployeeDetails WHERE EmployeeId = @EmployeeId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@EmployeeId", employeeId);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var employee = new
                    {
                        EmployeeId = reader["EmployeeId"].ToString(),
                        EmployeeName = reader["EmployeeName"].ToString(),
                        Department = reader["Department"].ToString(),
                        Age = Convert.ToInt32(reader["Age"])
                    };

                    return Ok(employee);
                }

                return NotFound(new { Message = "Employee not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employee by ID");
                return StatusCode(500, new { Message = "Internal Server Error", Error = ex.Message });
            }
        }

        // 🔹 UPDATE: Update Employee Data
        [HttpPut("UpdateEmployee/{employeeId}")]
        

        public IActionResult UpdateEmployee(string employeeId, [FromBody] EmployeeInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                string query = @"UPDATE EmployeeDetails
                                 SET EmployeeName = @EmployeeName,
                                     Department = @Department,
                                     Age = @Age
                                 WHERE EmployeeId = @EmployeeId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@EmployeeId", employeeId);
                command.Parameters.AddWithValue("@EmployeeName", input.EmployeeName);
                command.Parameters.AddWithValue("@Department", input.Department != null ? (object)input.Department : DBNull.Value);
                command.Parameters.AddWithValue("@Age", input.Age);

                int rows = command.ExecuteNonQuery();

                return rows > 0
                    ? Ok(new { Message = "Employee updated successfully" })
                    : NotFound(new { Message = "Employee not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee");
                return StatusCode(500, new { Message = "Internal Server Error", Error = ex.Message });
            }
        }

        // 🔹 DELETE: Delete Employee
        [HttpDelete("DeleteEmployee/{employeeId}")]
        

        public IActionResult DeleteEmployee(string employeeId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                string query = "DELETE FROM EmployeeDetails WHERE EmployeeId = @EmployeeId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@EmployeeId", employeeId);

                int rows = command.ExecuteNonQuery();

                return rows > 0
                    ? Ok(new { Message = "Employee deleted successfully" })
                    : NotFound(new { Message = "Employee ID not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee");
                return StatusCode(500, new { Message = "Internal Server Error", Error = ex.Message });
            }
        }
    }
}