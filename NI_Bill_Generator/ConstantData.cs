using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NI_Bill_Generator
{
   public static class ConstantData
    {
       public static double LFC_rate;
        public static int billNumber;
        public static string customerNumber;
        public static string contractType;
        public static string subDivisionName;
        public static string waterResource;
        public static string customerName;
        public static string customerAddress;
        public static string phoneNumber;
        public static string billRateType;
        public static double sanctionQuota;
        public static Boolean meterInstalled;
        public static Boolean agreementDone;
        public static int meterNumber;
        public static int currentMeterReading;
        public static int prevMeterReading;
        public static int unitsConsumed;
        public static int totalUnitsConsumed;
        public static string billStartDate;
        public static string billEndDate;
        public static double billAmount;
        public static string lastDateforBillPayment;
        public static double waterCharges;
        public static double localTax;
        public static double totalBillAmount;
        public static int previousPaymentDue;
        public static double otherCharges;
        public static double finalAmount;
        public static double amountAfterDueDate;
        public static double seasonalConsumption;
        public static int billDays;

       public static void clearConstants()
       {

       }

        public static String getConnectionString()
        {
            return (ConfigurationManager.ConnectionStrings["NI_Bill_Generator.Properties.Settings.BillingDatabaseConnectionString"].ConnectionString);

        }
       
    }
}
