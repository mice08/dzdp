using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using DianPing.Admin.Job.workflow;
using Microsoft.Practices.EnterpriseLibrary.Data;
using DianPing.Framework.Data;
using DianPing.Common;
using DianPing.Framework;
using System.Data;
using System.Data.Common;
using System.Configuration;

namespace DianPing.Admin.Job
{
    class Program
    {
        static readonly Database DB = QueryExecutor.CreateDatabase("DianPingDatabase");
        static void Main(string[] args)
        {
            DateTime dt = DateTime.Now;
            var sqLstr=new StringBuilder();
            sqLstr.Append(" SELECT ProcInstID, TotalAmount,ExpenseDate, ParticipantNum, ParticipantName, FoodChargeTimes, FoodChargeValue, TrafficChargeTimes, TrafficChargeValue," +
                          " WeekendFoodTimes, WeekendFoodValue, WeekendTrafficTimes, WeekendTrafficValue) FROM FS_HistoryOvertime");
            
            DataSet ds;
            using (DbCommand cmd = DB.GetSqlStringCommand(sqLstr.ToString()))
            {
                ds = DB.ExecuteDataSet(cmd);
            }
            if (ds!=null&&ds.Tables[0]!=null&&ds.Tables[0].Rows.Count>0)
            {
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    int procInstID=ds.Tables[0].Rows[i]["ProcInstID"].ToString().ToInteger();
                    var proxy = new WorkFlowServiceSoapClient();
                    K2StatusDto[] dtos=proxy.GetProcessStatus(procInstID, null, "test");
                    if (dtos!=null&&dtos.Length>0)
                    {
                        foreach (var k2StatusDto in dtos)
                        {
                           if(k2StatusDto.Activity=="流程结束")
                           {
                               DoInitData(ds.Tables[0].Rows[i]);
                           } 
                        }
                    }
                }
            }
          
        }

        private static void DoInitData(DataRow dataRow)
        {
            var expenseNo = "A" + dataRow["ProcInstID"].ToString();
            var proposeDate = dataRow["ExpenseDate"].ToString()+"-01";
            var totalAmount = dataRow["TotalAmount"].ToString().ToDecimal();
            var sqLstr = new StringBuilder();
            sqLstr.Append("INSERT INTO FS_ExpenseBaseInfo " +
                          "( ExpenseNo, ExpenseCatalog, ProposerNo, ProposerName, " +
                          "CostDepartmentId, CostCity, CostCompany, PayeeId, ExpenseReason, BizType, OutBizId, " +
                          "AddLoginId, UpdateLoginId, ADDTIME, UpdateTime, BudgetTypeId, PayeeType, " +
                          "ExpenseTotalAmount, HasAttachment, ProposeDate, STATUS)");
            sqLstr.Append("VALUES (" + expenseNo + ", 1, " + dataRow["ParticipantNum"].ToString() + "," +
                          dataRow["ParticipantName"].ToString() + ", 0, '上海', '汉海', " +
                          "" + dataRow["ParticipantNum"].ToString() + ", '历史加班费报销', 1, " + expenseNo +
                          ", -1, -1, NOW(), NOW(), 1, 1,  " + totalAmount + ", 0, " + proposeDate + ", 1);");
            sqLstr.Append(
                "INSERT INTO FS_ExpenseItem ( ExpenseTypeId, ExpenseAmount, ExpenseBeginDate, ExpenseEndDate, " +
                "ExpenseDescription, ExpenseBaseId, ADDTIME, UpdateTime, STATUS)");
            sqLstr.Append("VALUES (1, " + totalAmount + ", " + proposeDate + ", " + proposeDate + ", " +
                          "'历史加班费报销', LAST_INSERT_ID(), NOW(), NOW(), 1);");
            sqLstr.Append(
                "INSERT INTO FS_WotExpenseExtendInfo (RegularWotTimes, RegularTaxiTimes, OtherWotTimes, OtherTaxiTimes, ExpenseItemId, " +
                "ADDTIME, UpdateTime, WotInvoiceAmount, TaxiInvoiceAmount, RegularWotAmount, RegularTaxiAmount, OtherWotAmount, OtherTaxiAmount," +
                " SuggestRegularWotTimes, SuggestRegularTaxiTimes, SuggestOtherWotTimes, SuggestOtherTaxiTimes, STATUS)");
            sqLstr.Append("VALUES (" + dataRow["FoodChargeTimes"].ToString() .ToInteger()+ ", "+dataRow["TrafficChargeTimes"].ToString() .ToInteger()+", " +
                          ""+dataRow["WeekendFoodTimes"].ToString() .ToInteger()+", "+dataRow["WeekendTrafficTimes"].ToString() .ToInteger()+", " +
                          "LAST_INSERT_ID(), NOW(), NOW(), 0, 0, " + dataRow["FoodChargeValue"].ToString() .ToDecimal()+ ", " +
                          "" + dataRow["TrafficChargeValue"].ToString().ToDecimal() + ", " + dataRow["WeekendFoodValue"].ToString().ToDecimal() + ", " +
                          "" + dataRow["WeekendTrafficValue"].ToString().ToDecimal() + ", 0, 0, 0, 0, 1);");
            DbCommand cmd = new SqlCommand(sqLstr.ToString());
            DB.ExecuteNonQuery(cmd);
        }
    }
}
