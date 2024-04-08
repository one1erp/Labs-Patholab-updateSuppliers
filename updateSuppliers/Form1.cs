using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OracleClient;
using System.Configuration;

namespace updateSuppliers
{
    public partial class Form1 : Form
    {

        int supplierIdDB;
        string drLicenseDB;
        string firstNameDB;
        string lastNameDB;
        string doctorRecordDB;
        string mailDB;
        string[] doctorDetailsExcel;
        long supplierID;
        string docrorIdExcel;
        string firstNameExcel;
        string lastNameExcel;
        string fullNameExcel;
        string title = "'ד\"ר'";
        string mailExcel;
        string doctorLicenseExcel;
        string doctorProficency;
        string[] fullNameArray;
        int countLogEntries = 1;
        StreamWriter sw;


        public Form1()
        {
            InitializeComponent();
        }

        int existsCounter = 0;
        int notExistsCounter = 0;
        int updatedCounter = 0;
        private void Button1_Click(object sender, EventArgs e)
        {

            textBox1.Text = "running...";

            // Set database connection and query.

            Dictionary<string, string> doctorDict = new Dictionary<string, string>();
            OracleConnection connection = new OracleConnection(ConfigurationManager.AppSettings["connectionString"]);
            OracleCommand cmd = new OracleCommand();
            OracleDataReader dr = null;

            cmd.Connection = connection;
            cmd.CommandText = "SELECT * FROM supplier s INNER JOIN SUPPLIER_USER su ON s.SUPPLIER_ID = su.SUPPLIER_ID";
            cmd.CommandType = CommandType.Text;

            extractTable(cmd, connection, dr, ref doctorDict);

            // Read Asuta file.
            using (StreamReader reader = new StreamReader(ConfigurationManager.AppSettings["excelPath"], Encoding.UTF8))
            {
                sw = new StreamWriter(ConfigurationManager.AppSettings["logPath"], true); // True - append new text with old one. False - override.

                reader.ReadLine(); // Skip headline.

                string row = String.Empty;

                try
                {
                    connection.Open();
                    dr = cmd.ExecuteReader();
                }
                catch (Exception EX)
                {

                    MessageBox.Show(EX.Message);
                }

                while ((row = reader.ReadLine()) != null)
                {
                    try
                    {
                        doctorDetailsExcel = row.Split(',');
                        doctorLicenseExcel = doctorDetailsExcel[1];
                        fullNameExcel = doctorDetailsExcel[3];
                        docrorIdExcel = doctorDetailsExcel[2];
                        mailExcel = doctorDetailsExcel[7];
                        doctorProficency = doctorDetailsExcel[9];



                        // Check for valid license id (not empty).
                        if (doctorLicenseExcel.Trim().Equals(String.Empty)) continue;

                        sw.WriteLine(string.Format("Get license number {0} from excel", doctorLicenseExcel));

                        // Check if the doctor is already in the database.
                        if (doctorDict.ContainsKey(doctorLicenseExcel))
                        {
                            existsCounter++;
                            sw.WriteLine(string.Format("Get license number {0} from excel Exists in DB", doctorLicenseExcel));


                            doctorDict.TryGetValue(doctorLicenseExcel, out doctorRecordDB);
                            fullNameArray = doctorRecordDB.Split('~');
                            firstNameDB = fullNameArray[0];
                            lastNameDB = fullNameArray[1];
                            mailDB = fullNameArray[2];

                            // Check for valid name.
                            if (((firstNameDB + " " + lastNameDB).Trim().Equals(fullNameExcel)) || ((lastNameDB + " " + firstNameDB).Trim().Equals(fullNameExcel)))
                            {
                                updatedCounter++;
                                // If the name is valid, update relevant fields.
                                cmd.CommandText = string.Format("UPDATE supplier_user SET U_ID_NBR='{0}' WHERE U_LICENSE_NBR='{1}'", addLeadingZero(docrorIdExcel), doctorLicenseExcel);
                                sw.WriteLine(cmd.CommandText);
                                cmd.ExecuteNonQuery();

                                // will only change email if the previous one was empty.
                                if (mailDB.Trim().Equals(String.Empty))
                                {
                                    cmd.CommandText = string.Format("UPDATE supplier_user SET U_EMAIL_ADDRESS='{0}' WHERE U_LICENSE_NBR='{1}'", mailExcel, doctorLicenseExcel);
                                    sw.WriteLine(cmd.CommandText);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                // Log doctor information to log.txt file - will log only doctors who's license id is in the database but with different name.
                                string invalidRow = string.Format(
@"{0})
Doctor ID in Excel: {1}.
Doctor license number: {2}.
Doctor name in Nautilus: {3} {4}.
Doctor name in excel: {5}.
Doctor mail in excel: {6}.
Doctor proficency in excel: {7}.{8}{8}", countLogEntries++, docrorIdExcel, doctorLicenseExcel, firstNameDB, lastNameDB, fullNameExcel, mailExcel, doctorProficency, Environment.NewLine);
                                sw.WriteLine(invalidRow);
                                sw.Flush();

                            }
                        }
                        else
                        {
                            notExistsCounter++;
                            sw.WriteLine(string.Format("Get license number {0} from excel Not Exists in DB", doctorLicenseExcel));
                            // Add doctor information to database - will get here if the doctor is not in the database.

                            cmd.CommandText = "SELECT sq_supplier.NEXTVAL FROM dual";
                            OracleDataReader newSupplierID = cmd.ExecuteReader();
                            newSupplierID.Read();
                            supplierID = Convert.ToInt64(newSupplierID["NEXTVAL"]); // Unique supplier_id.

                            string[] fullNameArray = fullNameExcel.Split(new[] { ' ' }, 2);
                            firstNameExcel = fullNameArray[0];
                            lastNameExcel = fullNameArray[1];

                            cmd.CommandText = "INSERT INTO supplier (SUPPLIER_ID, NAME, VERSION,VERSION_STATUS) " +
                                string.Format("VALUES ({0}, {1}, 1,'A')", supplierID.ToString(), doctorLicenseExcel);
                            sw.WriteLine(cmd.CommandText);
                            cmd.ExecuteNonQuery();
                            cmd.CommandText = "INSERT INTO supplier_user (SUPPLIER_ID, U_DEGREE, U_FIRST_NAME, U_LAST_NAME, U_ID_NBR, U_LICENSE_NBR, U_PROFICENCY, U_EMAIL_ADDRESS) " +
                                string.Format("VALUES ({0}, {1}, '{2}', '{3}', '{4}', '{5}', '{6}', '{7}')", supplierID.ToString(), title, firstNameExcel, lastNameExcel, addLeadingZero(docrorIdExcel), doctorLicenseExcel, doctorProficency, mailExcel);
                            sw.WriteLine(cmd.CommandText);
                            cmd.ExecuteNonQuery();


                            sw.WriteLine(cmd.CommandText);

                            // Add doctor to dictionary.
                            doctorDict.Add(doctorLicenseExcel, firstNameExcel + "~" + lastNameExcel + "~" + mailExcel);
                        }
                    }
                    catch (Exception EX)
                    {
                        textBox1.Text = "problem occurred.";
                        MessageBox.Show(EX.Message);
                        continue;
                    }
                }
                textBox1.Text = "done.";
                sw.Close();


                textBox1.Text += "  Exists=" + existsCounter + "  Not exists=" + notExistsCounter + "  Updated=" + updatedCounter;
            }
        }


        // This method load a database to a dictionary with license id as key.
        private void extractTable(OracleCommand cmd, OracleConnection connection, OracleDataReader dr, ref Dictionary<string, string> doctorDict)
        {
            try
            {
                connection.Open();
                dr = cmd.ExecuteReader();
            }
            catch (Exception EX)
            {

                MessageBox.Show(EX.Message);
            }

            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    try
                    {
                        supplierIdDB = dr.GetInt32(0);
                        drLicenseDB = String.Empty;
                        lastNameDB = String.Empty;
                        firstNameDB = String.Empty;
                        mailDB = String.Empty;

                        if (dr.IsDBNull(2)) continue; // Move to the next row if the doctor dont have License_ID.

                        drLicenseDB = dr.GetString(2);
                        if (!dr.IsDBNull(11)) firstNameDB = Convert.ToString(dr["U_FIRST_NAME"]);
                        if (!dr.IsDBNull(12)) lastNameDB = Convert.ToString(dr["U_LAST_NAME"]);
                        if (!dr.IsDBNull(16)) mailDB = Convert.ToString(dr["U_EMAIL_ADDRESS"]);


                        doctorRecordDB = firstNameDB + "~" + lastNameDB + "~" + mailDB;
                        doctorRecordDB = doctorRecordDB.Trim();

                        if (drLicenseDB != String.Empty && !doctorDict.ContainsKey(drLicenseDB))
                        {
                            doctorDict.Add(drLicenseDB, doctorRecordDB);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                        continue;
                    }
                }

                connection.Close();
                dr.Close();
            }
        }

        // Adds leading zeros to the id if necessary.
        private string addLeadingZero(string id)
        {
            string zeros = String.Empty;
            for (int i = 0; i < 9 - id.Length; i++)
            {
                zeros += "0";
            }
            return zeros + id;
        }
    }
}
