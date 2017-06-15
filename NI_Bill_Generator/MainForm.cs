using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;





namespace NI_Bill_Generator
{
    public partial class MainForm : Form
    {
        SqlConnection connection;
        SqlDataAdapter adapter;
        //string sanctionQuota;
        
        //constructor
        public MainForm()
        {
            InitializeComponent();
            //Change Form title
            this.Text = "NI BILL GENERATION";
              //Maximize the form
            this.WindowState = FormWindowState.Maximized;
            //Change Form background color 
            this.BackColor = Color.White;

            checkMeterInstalled.Checked = false;
            textBox1.Enabled = false;
            dueAmount.Enabled = false;

        }

       

        private void Form1_Load(object sender, EventArgs e)
        {
            
            //fetching connection string from ConstantData class  
            using(connection = new SqlConnection(ConstantData.getConnectionString()))
            using ( adapter = new SqlDataAdapter("select consumer_name from Consumer", connection))
            {
                //connection.Open();
                DataTable consumer_col = new DataTable();
                adapter.Fill(consumer_col);

                //Inserting extra row in DataTable
                DataRow row = consumer_col.NewRow();
                row["consumer_name"] = "Select Consumer";
                consumer_col.Rows.InsertAt(row,0);

                consumerList.DisplayMember = "consumer_name";
                consumerList.DataSource = consumer_col;

            }


        }
        
        private void clearForm()
        {
            //clear forms on change of customer 
        }
     
        private void getConsumerDetails(object sender, EventArgs e)
        {
            clearForm();
            
            string name = consumerList.Text;
            addrText.Text = "";
            waterText.Text = "";
            quotaText.Text = "";
            using(connection = new SqlConnection(ConstantData.getConnectionString()))
            {
                DataTable consumerDetail = new DataTable();
                connection.Open();
                SqlDataReader myReader = null;
                SqlCommand myCommand = new SqlCommand("select * from Consumer where consumer_name = '" + name +"'", connection);

                myReader = myCommand.ExecuteReader();


                if (myReader.Read())
                {
                    addrText.Text = (myReader["Address"].ToString());
                    waterText.Text = (myReader["water_source"].ToString());
                    quotaText.Text = (myReader["sanction_quota"].ToString());

                    //storing data for further referance
                    ConstantData.customerName = name;
                    ConstantData.customerAddress = myReader["Address"].ToString();
                    ConstantData.waterResource = myReader["water_source"].ToString();
                    ConstantData.sanctionQuota = double.Parse(myReader["sanction_quota"].ToString());

                    //take contact details from db if required
   
                }
                else
                {
                    //MessageBox.Show("No Record Found.");
                }

               
            }
            

        }

        private void addDetailstoList(object sender, EventArgs e)
        {

            string contract = contractType.Text;
            double kharip_rate = 0;
            double rabbi_rate = 0;
            double hot_rate = 0;

            using (connection = new SqlConnection(ConstantData.getConnectionString()))
            {
                DataTable consumerDetail = new DataTable();
                connection.Open();
                SqlDataReader myReader = null;
                SqlCommand myCommand = new SqlCommand("select * from Contract where contract_type = '" + contract + "'", connection);
                //MessageBox.Show(contract);
                myReader = myCommand.ExecuteReader();

                if (myReader.Read())
                {
                    if (!checkMeterInstalled.Checked)
                    {
                        if (int.Parse(meter1.Text) > int.Parse(meter2.Text))
                        {
                            MessageBox.Show("Please enter correct details");
                        }
                    }
                    //addrText.Text = (myReader["Address"].ToString());
                    kharip_rate = float.Parse((myReader["kharip_rate"]).ToString(), System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                    rabbi_rate = float.Parse((myReader["rabbi_rate"]).ToString(), System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                    hot_rate = float.Parse((myReader["hot_season_rate"]).ToString(), System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                    //Adding record in bill table
                    ConstantData.contractType = contract;
                    updateBill(kharip_rate, rabbi_rate, hot_rate);
                }
                else
                {
                    MessageBox.Show("Unable to fetch contract details");
                }
            }

            


        }

        private void updateBill(double kharip_rate,double rabbi_rate,double hot_rate)
        {
            double seasonRate = 0, baseWaterCharge = 0, localFund, totalBill, baseWaterChargeforLFC = 0;
            string seasonType = seasonBox.Text;
            string billPeriod = dateTimePicker1.Value.ToShortDateString() + " - " + dateTimePicker2.Value.ToShortDateString();
            ListViewItem bill, paymentRow;
            int days = ConstantData.billDays = (dateTimePicker2.Value - dateTimePicker1.Value).Days + 1;            
            double sanctionQuota = double.Parse(quotaText.Text);
            double perSeasonQuota = ConstantData.seasonalConsumption = Math.Round((((sanctionQuota * 100000) / 365) * days),6);

            ConstantData.billStartDate = dateTimePicker1.Value.Date.ToString();
            ConstantData.billEndDate = dateTimePicker2.Value.Date.ToString();
            
            //added new column to database for LFC_rate 
            string consumer = consumerList.Text;
            DataTable consumerDetail = new DataTable();
            connection.Open();
            SqlDataReader myReader = null;
            SqlCommand myCommand = new SqlCommand("select LFC_rate from Contract where consumer_name = '" + consumer + "'", connection);
            //MessageBox.Show(contract);
            myReader = myCommand.ExecuteReader();
            myReader.Read();
            double LFC_rate = double.Parse(myReader["LFC_rate"].ToString());
            ConstantData.LFC_rate = LFC_rate;

            if (!checkMeterInstalled.Checked)
            {
                ConstantData.meterInstalled = true;
                
                int meterFrom = ConstantData.prevMeterReading = int.Parse(meter1.Text);
                int meterTo = ConstantData.currentMeterReading = int.Parse(meter2.Text);
                 
                
                int seasonalConsumption = ConstantData.unitsConsumed = (int)Math.Ceiling((double)(meterTo - meterFrom) / 10);
                //MessageBox.Show(kharip_rate.ToString());
                bill = new ListViewItem(billPeriod);
                bill.SubItems.Add(days.ToString());
                bill.SubItems.Add(meterFrom.ToString());
                bill.SubItems.Add(meterTo.ToString());
                bill.SubItems.Add(seasonalConsumption.ToString());

                
                bill.SubItems.Add(perSeasonQuota.ToString());
                if (seasonType == "Kharip")
                {
                    seasonRate = kharip_rate;
                    bill.SubItems.Add((Math.Round((kharip_rate),2)).ToString());
                    
                }
                else if (seasonType == "Rabbi")
                {
                    seasonRate = rabbi_rate;
                    bill.SubItems.Add((Math.Round((rabbi_rate), 2)).ToString());
                }
                else if (seasonType == "Hot Season")
                {
                    seasonRate = hot_rate;
                    bill.SubItems.Add((Math.Round((hot_rate), 2)).ToString());
                }
                else if (seasonType == "")
                {
                    MessageBox.Show("PLEASE SELECT SEASON TYPE");
                }
                billList.Items.Add(bill);

                paymentRow = new ListViewItem(billPeriod);

                //check for 90,110,quota
                double finalConsumption = 0;
                finalConsumption = Math.Ceiling(checkPercentage(seasonalConsumption, perSeasonQuota));
                paymentRow.SubItems.Add(finalConsumption.ToString());
                if(checkNoAgreement.Checked)
                {
                    baseWaterCharge = Math.Round((seasonRate * finalConsumption * 1.5), 2);
                }
                else if(agreementExpired.Checked) 
                {
                    baseWaterCharge = Math.Round((seasonRate * finalConsumption * 1.25), 2);
                }
                else
                {
                    baseWaterCharge = Math.Round((seasonRate * finalConsumption), 2);
                }

                
                paymentRow.SubItems.Add(baseWaterCharge.ToString());
                baseWaterChargeforLFC = Math.Round((seasonRate * finalConsumption), 2);
                localFund = Math.Round((baseWaterChargeforLFC * 0.2), 2);
                paymentRow.SubItems.Add((localFund).ToString());
                totalBill = Math.Round((baseWaterCharge + localFund), 2);
                paymentRow.SubItems.Add("0.0");
                paymentRow.SubItems.Add("0.0");
                paymentRow.SubItems.Add((totalBill).ToString());
                lstViewPayment.Items.Add(paymentRow);
                //MessageBox.Show(baseWaterCharge.ToString());

                ConstantData.waterCharges = baseWaterCharge;
                ConstantData.localTax = localFund;
                ConstantData.billAmount = totalBill;
            }
            else
            {
                ConstantData.meterInstalled = false;
                if (seasonType == "Kharip")
                {
                    seasonRate = kharip_rate;                    
                }
                else if (seasonType == "Rabbi")
                {
                    seasonRate = rabbi_rate;                   
                }
                else if (seasonType == "Hot Season")
                {
                    seasonRate = hot_rate;                   
                }
                else if (seasonType == "")
                {
                    MessageBox.Show("PLEASE SELECT SEASON TYPE");
                }

                paymentRow = new ListViewItem(billPeriod);
                paymentRow.SubItems.Add(Math.Ceiling(perSeasonQuota * 1.25).ToString());
                //added as meter not working or installed so 125% of Quota
                perSeasonQuota = Math.Ceiling(perSeasonQuota * 1.25);
                //multiplied by 1.25 as agreement is expired 
                double seasonRate1 = 0;
                if (agreementExpired.Checked)
                {
                    seasonRate1 = 1.25 * seasonRate;
                }
                double finalConsumption = Math.Round((perSeasonQuota * seasonRate1),2);
                paymentRow.SubItems.Add(finalConsumption.ToString());
                //calculate seperatly based on database
                localFund = Math.Round((perSeasonQuota * seasonRate * 0.2), 2);
                paymentRow.SubItems.Add((localFund).ToString());
                totalBill = Math.Round((finalConsumption + localFund), 2);
                paymentRow.SubItems.Add("0.0");
                paymentRow.SubItems.Add("0.0");
                paymentRow.SubItems.Add((totalBill).ToString());
                lstViewPayment.Items.Add(paymentRow);
                ConstantData.waterCharges = finalConsumption;
                ConstantData.localTax = localFund;
                ConstantData.billAmount = totalBill;
            }

            
        }


      private double checkPercentage(int seasonalConsumption, double sanctionQuota)
        {
            //sanctionQuota = sanctionQuota * 100000;
            
            double cunsumption = (100 * seasonalConsumption) / sanctionQuota;
            MessageBox.Show(seasonalConsumption.ToString() + "  " + sanctionQuota.ToString() + "  " + cunsumption);
            if (cunsumption < 90)
                return (sanctionQuota * .90);
            else if (cunsumption >= 90 && cunsumption < 110)
                return seasonalConsumption;
            else
                return seasonalConsumption;

         
        }


        private void calculateDate(object sender, EventArgs e)
        {
            //Do validation 

            if (dateTimePicker1.Text.Length == 0)
            {
                MessageBox.Show("PLEASE SELECT START DATE.");
            }
            else if (dateTimePicker2.Text.Length == 0)
            {
                MessageBox.Show("PLEASE SELECT END DATE.");
            }
            else if (dateTimePicker1.Value > dateTimePicker2.Value)
            {
                MessageBox.Show("PLEASE SELECT CORRECT DATE.");
            }
            else
            {
                int days = (dateTimePicker2.Value - dateTimePicker1.Value).Days + 1;
                label8.Text = (days.ToString() + "   Days");
            }
        }

       

        private void checkBillClick(object sender, EventArgs e)
        {
            
            ListViewItem item;
            int i, dueAmnt=0;
            double totalBill = 0, LFCCharge = 0;

            if(checkPaymentDue.Checked)
            {
                dueAmnt = int .Parse(dueAmount.Text);
                ConstantData.previousPaymentDue = dueAmnt;
            }
             
           for (i = 0; i < lstViewPayment.Items.Count; i++)
                {
                    item = lstViewPayment.Items[i];
                    totalBill += Math.Round(double.Parse(item.SubItems[2].Text.ToString()), 2);
                    LFCCharge += Math.Round(double.Parse(item.SubItems[3].Text.ToString()), 2);
                }
            
            //Adding the previous due if any 
            totalBill += dueAmnt;
            ConstantData.finalAmount = totalBill;
          
               /*
            if(!checkMeterInstalled.Checked)
            {
                sanctionQuota = float.Parse(quotaText.Text);
                perSeasonQuota = (int)((sanctionQuota * 100000) / 365) * days;
                totalBill = perSeasonQuota * 1.25 * rate;
            }
            */
            billAmount.Text =  totalBill.ToString();
            LFC.Text = LFCCharge.ToString();
            ConstantData.totalBillAmount = Math.Ceiling(totalBill + LFCCharge);
            totalAmount.Text = Math.Ceiling(totalBill + LFCCharge).ToString();
        }

        private void RemoveReordsFromList(object sender, EventArgs e)
        {
            if (billList.Items.Count > 0)
            {
                int i = 0;
                try
                {
                    for (i = 0; i < lstViewPayment.Items.Count; i++)
                    {
                        lstViewPayment.Items.Remove(lstViewPayment.Items[i]);
                    }
                    billList.Items.Remove(billList.SelectedItems[0]);
                }
                catch (ArgumentOutOfRangeException argException)
                {

                }

            }
        }

        private void validateMeterReading(object sender, KeyPressEventArgs e)
        {
            char ch = e.KeyChar;
            //check for non digit and backspace 
            if(!char.IsDigit(ch) && ch != 8 && ch != 46)
            {
                e.Handled = true;
            }
        }

        private void checkNoAgreement_CheckedChanged(object sender, EventArgs e)
        {
            if (checkNoAgreement.Checked)
            {
               
                checkMeterNotWorking.Enabled = false;
                checkMeterInstalled.Enabled = false;
                textBox1.Enabled = true;
            }
            else
            {
                
                checkMeterNotWorking.Enabled = true;
                checkMeterInstalled.Enabled = true;
                textBox1.Enabled = false;
            } 

        }

        private void checkPaymentDue_CheckedChanged(object sender, EventArgs e)
        {
            if(checkPaymentDue.Checked)
            {
                dueAmount.Enabled = true;
                //billGeneration.Enabled = true;

            }
            else if(!checkPaymentDue.Checked)
            {
                //billGeneration.Enabled = false;
                dueAmount.Enabled = false;
            }
           
        }

        private void checkMeterNotInstalled_CheckedChanged(object sender, EventArgs e)
        {
            if(!checkMeterInstalled.Checked)
            {
             
                label11.Enabled = true;
                label12.Enabled = true;
                button2.Enabled = true;
                billList.Enabled = true;
                label10.Enabled = true;
                seasonBox.Enabled = true;
                meter1.Enabled = true;
                meter2.Enabled = true;
                //lstViewPayment.Enabled = true;
            }
            else if(checkMeterInstalled.Checked)
            {
                label11.Enabled = false;
                label12.Enabled = false;
                button2.Enabled = false;
                billList.Enabled = false;
                label10.Enabled = false;
                meter1.Enabled = false;
                meter2.Enabled = false;
                //lstViewPayment.Enabled = false;
            }
                
        }

        private void checkMeterNotWorking_CheckedChanged(object sender, EventArgs e)
        {
            if (checkMeterNotWorking.Checked)
                checkMeterInstalled.Enabled = false;
            else
                checkMeterInstalled.Enabled = true;

        }

        private void billGeneration_Enter(object sender, EventArgs e)
        {
            billGeneration.Controls.Add(label4);
            billGeneration.Controls.Add(label5);
            billGeneration.Controls.Add(label6);
            billGeneration.Controls.Add(label7);
            billGeneration.Controls.Add(label8);
            billGeneration.Controls.Add(label11);
            billGeneration.Controls.Add(label12);
            billGeneration.Controls.Add(label13);
            billGeneration.Controls.Add(button1);
            billGeneration.Controls.Add(button2);
            billGeneration.Controls.Add(billList);
            billGeneration.Controls.Add(lstViewPayment);
        }

        private void generateDocumentedBill(object sender, EventArgs e)
        {
            string orderNo = DateTime.Now.Ticks.ToString().Substring(0, 6);
            string orderDate = DateTime.Now.ToString("dd MMM yyyy");
            string accountNo = ConstantData.customerNumber;
            string accountName = ConstantData.customerName;
            string branch = ConstantData.subDivisionName;
            string address = ConstantData.customerAddress;
            string docName = accountName + "_NI_BILL" + ".pdf";
            //Document pdfDoc = new Document(iTextSharp.text.PageSize.LETTER, 10, 10, 42, 35);
            //Document pdfDoc = new Document(PageSize.A4, 10f, 10f, 10f, 0f);

            
            String strSelectUserListBuilder = "<html lang=\"en\">" +
                                "<head>"+
                                "<meta charset=\"utf-8\" />" +
                                "<title></title>"+
                                "</head>"+
                                "<body>" +
                                "<table border=\"1\"  style=\"width:100%\">"+
                                " <tr style=\"text-align:center\">"+
                                "<td colspan=\"2\">" +
                                "<h1>GOVERNMENT OF MAHARASHTRA</h1>"+
                                "<h1>WATER RESOURCES DEPARTMENT</h1>"+
                                "<h1>PUNE IRRIGATION DIVISION</h1>"+
                                "</td>"+
                                "</tr>"+
                                "<tr>" +
                                "<td style=\"width:50%\">" +              
                                    "<table style=\"width:100%\">" +
                                        "<tr>" +
                                            "<td> <h2>Customer Details</h2></td>"+
                                        "</tr>"+
                                        "<tr>"+
                                            "<td>Consumer Name :"+ConstantData.customerName+" </td>"+
                                        "</tr>"+
                                         "<tr>"+
                                            "<td>Consumer Address :"+ConstantData.customerAddress+" </td>"+
                                        "</tr>"+
                                         "<tr>"+
                                            "<td>Consumer Number: "+ "</td>"+
                                        "</tr>"+
                                        "<tr>" +
                                            "<td>Sub Division and Section: " +   "</td>" +
                                        "</tr>" +
                                        "<tr>" +
                                            "<td>Water Source: " + ConstantData.waterResource + "</td>" +
                                        "</tr>" +
                                        "<tr>" +
                                            "<td>Yearly Sanction Quota: " + ConstantData.sanctionQuota + " Mm3</td>" +
                                        "</tr>" +
                                        "<tr>" +
                                            "<td>Yearly Purpose of water use: " + ConstantData.contractType + "</td>" +
                                        "</tr>" +
                                        "<tr>"+
                                            "<td>"+
                                                "<table border=\"1\">"+
                                                    "<thead>"+
                                                        "<tr>"+
                                                            "<td>Meter reading from </td>"+
                                                            "<td>Meter reading to</td>"+
                                                            "<td>Units Consumed</td>"+
                                                        "</tr>"+
                                                    "</thead>"+
                                                    "<tbody>"+
                                                        "<tr>"+
                                                            "<td>" + ConstantData.prevMeterReading + "</td>" +
                                                            "<td>" + ConstantData.currentMeterReading + "</td>" +
                                                            "<td>"+ConstantData.unitsConsumed+"</td>"+
                                                        "</tr>"+
                                                    "</tbody>"+
                                               " </table>"+
                                            "</td>"+
                                        "</tr>"+
                                    "</table>"+
                                "</td>"+
                                "<td style=\"width:50%\">"+
                                    "<table style=\"width:100%;height:100%; \">"+
                                      "<tr><td>Bill Start Date </td><td style=\"text-align:right\">" + ConstantData.billStartDate + "</td></tr>" +
                                      "<tr><td>Bill End Date </td><td style=\"text-align:right\">" + ConstantData.billEndDate + "</td></tr>" +
                                      "<tr><td>Total days </td><td style=\"text-align:right\">" + ConstantData.billDays + "</td></tr>   " +             
                                    "</table>"+
                               "</td>"+
                            "</tr>"+
                            "<tr style=\"text-align:right\">"+
                                "<td colspan=\"2\" >"+
                                    "Billable Amount:"+ ConstantData.finalAmount +
                                "</td>"+
                            "</tr>"+
                             "<tr style=\"text-align:right\">"+
                                "<td colspan=\"2\" >"+
                                    "Tax Total: "+ConstantData.localTax +
                                "</td>"+
                            "</tr>"+
                             "<tr style=\"text-align:right\">"+
                                "<td colspan=\"2\" >"+
                                    "Total Amount: " + ConstantData.totalBillAmount +
                                "</td>"+
                            "</tr>"+
                            "</table>"+
                            "</body>" +
                        "</html>";

            String htmlText = strSelectUserListBuilder.ToString();
            Document document = new Document();
            
            try{
                
                PdfWriter.GetInstance(document, new FileStream(docName, FileMode.Create));
                document.Open();
                iTextSharp.text.html.simpleparser.HTMLWorker hw =
                             new iTextSharp.text.html.simpleparser.HTMLWorker(document);
                hw.Parse(new StringReader(htmlText));
                document.Close();
                
              /*
             
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, new FileStream(docName, FileMode.Create));
                pdfDoc.Open();
                
                string imageURL = "../../Resources/IndiaEmblem_SatyamevaJayate.png";
                iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(imageURL);
                logo.Alignment = Element.ALIGN_TOP;
            
                    Paragraph title = new Paragraph("GOVERNMENT OF MAHARASHTRA\n WATER RESOURCES DEPARTMENT\n PUNE IRRIGATION DIVISION");
                    title.Alignment = Element.ALIGN_CENTER;
                
                    pdfDoc.Add(title);
                    pdfDoc.Add(logo);
               
                */
            } catch(Exception ex)
            {

            }
            finally
            {
                document.Close();
            }
        }

        private void agreementExpired_CheckedChanged(object sender, EventArgs e)
        {

        }

      

     
              
    }
}
