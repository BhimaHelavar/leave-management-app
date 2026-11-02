using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "people");

            migrationBuilder.EnsureSchema(
                name: "balances");

            migrationBuilder.EnsureSchema(
                name: "leave");

            migrationBuilder.EnsureSchema(
                name: "notify");

            migrationBuilder.CreateTable(
                name: "AuditEvent",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ActorEmployeeId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    EntityId = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    DetailJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveType",
                schema: "balances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    DaysPerYear = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    AccrualMode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    CarryForwardMaxDays = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    CarryForwardExpiryMonth = table.Column<int>(type: "int", nullable: true),
                    CarryForwardExpiryDay = table.Column<int>(type: "int", nullable: true),
                    MinNoticeWorkingDays = table.Column<int>(type: "int", nullable: false),
                    MaxConsecutiveDays = table.Column<int>(type: "int", nullable: true),
                    ProbationBlockMonths = table.Column<int>(type: "int", nullable: false),
                    AllowHalfDay = table.Column<bool>(type: "bit", nullable: false),
                    MedicalCertAfterDays = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveType", x => x.Id);
                    table.CheckConstraint("CK_LeaveType_AccrualMode", "[AccrualMode] IN ('None', 'Monthly', 'Yearly', 'Manual')");
                    table.CheckConstraint("CK_LeaveType_CarryForwardExpiry", "([CarryForwardExpiryMonth] IS NULL AND [CarryForwardExpiryDay] IS NULL) OR ([CarryForwardExpiryMonth] BETWEEN 1 AND 12 AND [CarryForwardExpiryDay] BETWEEN 1 AND 31)");
                    table.CheckConstraint("CK_LeaveType_Numbers", "([DaysPerYear] IS NULL OR [DaysPerYear] >= 0) AND [CarryForwardMaxDays] >= 0 AND [MinNoticeWorkingDays] >= 0 AND ([MaxConsecutiveDays] IS NULL OR [MaxConsecutiveDays] > 0) AND [ProbationBlockMonths] >= 0 AND ([MedicalCertAfterDays] IS NULL OR [MedicalCertAfterDays] > 0)");
                });

            migrationBuilder.CreateTable(
                name: "Location",
                schema: "people",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    TimeZoneId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Location", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                schema: "notify",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedMessage",
                schema: "notify",
                columns: table => new
                {
                    Consumer = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedMessage", x => new { x.Consumer, x.MessageId });
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                schema: "people",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExternalId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    ManagerId = table.Column<int>(type: "int", nullable: true),
                    JoinDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employee_Employee_ManagerId",
                        column: x => x.ManagerId,
                        principalSchema: "people",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Location_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "people",
                        principalTable: "Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Holiday",
                schema: "balances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    HolidayDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holiday", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Holiday_Location_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "people",
                        principalTable: "Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRole",
                schema: "people",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeRole", x => new { x.EmployeeId, x.Role });
                    table.CheckConstraint("CK_EmployeeRole_Role", "[Role] IN ('Manager', 'HR', 'Admin')");
                    table.ForeignKey(
                        name: "FK_EmployeeRole_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "people",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyKey",
                schema: "leave",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    KeyValue = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    RequestHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyKey", x => new { x.EmployeeId, x.KeyValue });
                    table.ForeignKey(
                        name: "FK_IdempotencyKey_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "people",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveBalance",
                schema: "balances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    LeaveYear = table.Column<short>(type: "smallint", nullable: false),
                    Available = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalance", x => x.Id);
                    table.CheckConstraint("CK_LeaveBalance_Available", "[Available] >= 0");
                    table.ForeignKey(
                        name: "FK_LeaveBalance_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "people",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveBalance_LeaveType_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalSchema: "balances",
                        principalTable: "LeaveType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequest",
                schema: "leave",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    HalfDay = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    DaysCount = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false),
                    Status = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    LeaveYear = table.Column<short>(type: "smallint", nullable: false, computedColumnSql: "CONVERT(smallint, YEAR([StartDate]))", stored: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    DecidedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    DecisionComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequest", x => x.Id);
                    table.UniqueConstraint("AK_LeaveRequest_Id_EmployeeId", x => new { x.Id, x.EmployeeId });
                    table.CheckConstraint("CK_LeaveRequest_Dates", "[EndDate] >= [StartDate] AND YEAR([StartDate]) = YEAR([EndDate])");
                    table.CheckConstraint("CK_LeaveRequest_Days", "[DaysCount] > 0");
                    table.CheckConstraint("CK_LeaveRequest_DecidedAt", "([Status] = 'Pending' AND [DecidedAtUtc] IS NULL) OR ([Status] <> 'Pending' AND [DecidedAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_LeaveRequest_HalfDay", "[HalfDay] = 'None' OR [StartDate] = [EndDate]");
                    table.CheckConstraint("CK_LeaveRequest_HalfDayValue", "[HalfDay] IN ('None', 'FirstHalf', 'SecondHalf')");
                    table.CheckConstraint("CK_LeaveRequest_RejectComment", "[Status] <> 'Rejected' OR [DecisionComment] IS NOT NULL");
                    table.CheckConstraint("CK_LeaveRequest_Status", "[Status] IN ('Pending', 'Approved', 'Rejected', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_LeaveRequest_Employee_DecidedByEmployeeId",
                        column: x => x.DecidedByEmployeeId,
                        principalSchema: "people",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequest_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "people",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequest_LeaveType_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalSchema: "balances",
                        principalTable: "LeaveType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveBalanceEntry",
                schema: "balances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    LeaveYear = table.Column<short>(type: "smallint", nullable: false),
                    EntryType = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    Days = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: true),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByEmployeeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalanceEntry", x => x.Id);
                    table.CheckConstraint("CK_LeaveBalanceEntry_EntryType", "[EntryType] IN ('Grant', 'Accrual', 'CarryForward', 'Expiry', 'Deduction', 'Restore', 'Adjustment')");
                    table.ForeignKey(
                        name: "FK_LeaveBalanceEntry_Employee_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalSchema: "people",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveBalanceEntry_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "people",
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveBalanceEntry_LeaveRequest_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "leave",
                        principalTable: "LeaveRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveBalanceEntry_LeaveType_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalSchema: "balances",
                        principalTable: "LeaveType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequestDay",
                schema: "leave",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    LeaveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Slot = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequestDay", x => new { x.RequestId, x.LeaveDate, x.Slot });
                    table.CheckConstraint("CK_LeaveRequestDay_Slot", "[Slot] IN ('AM', 'PM')");
                    table.ForeignKey(
                        name: "FK_LeaveRequestDay_LeaveRequest_RequestId_EmployeeId",
                        columns: x => new { x.RequestId, x.EmployeeId },
                        principalSchema: "leave",
                        principalTable: "LeaveRequest",
                        principalColumns: new[] { "Id", "EmployeeId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvent_Action_Id",
                schema: "audit",
                table: "AuditEvent",
                columns: new[] { "Action", "Id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvent_ActorEmployeeId_Id",
                schema: "audit",
                table: "AuditEvent",
                columns: new[] { "ActorEmployeeId", "Id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Employee_Email",
                schema: "people",
                table: "Employee",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employee_ExternalId",
                schema: "people",
                table: "Employee",
                column: "ExternalId",
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_LocationId",
                schema: "people",
                table: "Employee",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_ManagerId",
                schema: "people",
                table: "Employee",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Holiday_LocationId_HolidayDate",
                schema: "balances",
                table: "Holiday",
                columns: new[] { "LocationId", "HolidayDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyKey_CreatedAtUtc",
                schema: "leave",
                table: "IdempotencyKey",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalance_EmployeeId_LeaveTypeId_LeaveYear",
                schema: "balances",
                table: "LeaveBalance",
                columns: new[] { "EmployeeId", "LeaveTypeId", "LeaveYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalance_LeaveTypeId",
                schema: "balances",
                table: "LeaveBalance",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceEntry_CreatedByEmployeeId",
                schema: "balances",
                table: "LeaveBalanceEntry",
                column: "CreatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceEntry_EmployeeId_LeaveTypeId_LeaveYear",
                schema: "balances",
                table: "LeaveBalanceEntry",
                columns: new[] { "EmployeeId", "LeaveTypeId", "LeaveYear" })
                .Annotation("SqlServer:Include", new[] { "Days" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceEntry_LeaveTypeId",
                schema: "balances",
                table: "LeaveBalanceEntry",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceEntry_RequestId",
                schema: "balances",
                table: "LeaveBalanceEntry",
                column: "RequestId",
                filter: "[RequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequest_DecidedByEmployeeId",
                schema: "leave",
                table: "LeaveRequest",
                column: "DecidedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequest_EmployeeId_StartDate",
                schema: "leave",
                table: "LeaveRequest",
                columns: new[] { "EmployeeId", "StartDate" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "LeaveTypeId", "Status", "DaysCount", "EndDate", "HalfDay" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequest_LeaveTypeId",
                schema: "leave",
                table: "LeaveRequest",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequest_Pending_StartDate",
                schema: "leave",
                table: "LeaveRequest",
                column: "StartDate",
                filter: "[Status] = 'Pending'")
                .Annotation("SqlServer:Include", new[] { "EmployeeId", "LeaveTypeId", "DaysCount", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequest_Status_StartDate",
                schema: "leave",
                table: "LeaveRequest",
                columns: new[] { "Status", "StartDate" })
                .Annotation("SqlServer:Include", new[] { "EmployeeId", "LeaveTypeId", "DaysCount" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestDay_LeaveDate",
                schema: "leave",
                table: "LeaveRequestDay",
                column: "LeaveDate")
                .Annotation("SqlServer:Include", new[] { "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestDay_RequestId_EmployeeId",
                schema: "leave",
                table: "LeaveRequestDay",
                columns: new[] { "RequestId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "UX_LeaveRequestDay_Employee_Date_Slot",
                schema: "leave",
                table: "LeaveRequestDay",
                columns: new[] { "EmployeeId", "LeaveDate", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveType_Code",
                schema: "balances",
                table: "LeaveType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveType_Name",
                schema: "balances",
                table: "LeaveType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Location_Name",
                schema: "people",
                table: "Location",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_MessageId",
                schema: "notify",
                table: "OutboxMessage",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_Unsent",
                schema: "notify",
                table: "OutboxMessage",
                column: "Id",
                filter: "[SentAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvent",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "EmployeeRole",
                schema: "people");

            migrationBuilder.DropTable(
                name: "Holiday",
                schema: "balances");

            migrationBuilder.DropTable(
                name: "IdempotencyKey",
                schema: "leave");

            migrationBuilder.DropTable(
                name: "LeaveBalance",
                schema: "balances");

            migrationBuilder.DropTable(
                name: "LeaveBalanceEntry",
                schema: "balances");

            migrationBuilder.DropTable(
                name: "LeaveRequestDay",
                schema: "leave");

            migrationBuilder.DropTable(
                name: "OutboxMessage",
                schema: "notify");

            migrationBuilder.DropTable(
                name: "ProcessedMessage",
                schema: "notify");

            migrationBuilder.DropTable(
                name: "LeaveRequest",
                schema: "leave");

            migrationBuilder.DropTable(
                name: "Employee",
                schema: "people");

            migrationBuilder.DropTable(
                name: "LeaveType",
                schema: "balances");

            migrationBuilder.DropTable(
                name: "Location",
                schema: "people");
        }
    }
}
