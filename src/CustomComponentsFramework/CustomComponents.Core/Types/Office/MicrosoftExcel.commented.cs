﻿//using System;
//using System.Linq;
//using Microsoft.Office.Interop.Excel;
//using System.Data.SqlClient;
//using System.Runtime.InteropServices;
//using System.Data.Common;
//using System.Diagnostics;
//using System.Collections.Generic;



//namespace EnhancedLibrary.Utilities.Business
//{
//    public static class MicrosoftExcel
//    {
//        public const string Excel03and07 = "Excel Files 2003|*.xls|Excel Files 2007|*.xlsx";



//        /// <summary>
//        ///     This class provide a way to import excel file to SQL database.
//        ///     You can set the override event, that will be called for each cell, giving you the oportunity 
//        ///     to override the cellValue.
//        /// </summary>
//        public class ImportXls2SQL
//        {
//            public enum MethodState { OK, EMPTY_FILE }

//            public String FilePath { get; private set; }
//            public String ConnectionString { get; private set; }
//            public String StoredProcedureName { get; private set; }

//            readonly Dictionary<int, string> m_parametersMapper = new Dictionary<int, string>();

//            public ImportXls2SQL(String FilePath, String ConnectionString, String StoredProcedureName)
//            {
//                if (FilePath.IsNE())
//                    throw new ArgumentNullException("FilePath");

//                if (ConnectionString.IsNE())
//                    throw new ArgumentNullException("ConnectionString");

//                if (StoredProcedureName.IsNE())
//                    throw new ArgumentNullException("StoredProcedureName");

//                this.FilePath = FilePath;
//                this.ConnectionString = ConnectionString;
//                this.StoredProcedureName = StoredProcedureName;
//            }


//            #region Public Methods



//            public MethodState PerformImport()
//            {
//                Application excelApp = null;
//                Workbook excelWB = null;
//                Worksheet excelWS = null;

//                uint processId = 0;

//                try
//                {
//                    excelApp = new Microsoft.Office.Interop.Excel.Application();
//                    processId = GetProcessIdFromHWnd(excelApp.Hwnd);

//                    excelWB = excelApp.OpenWorkBook(FilePath);
//                    excelWS = excelWB.GetWorkSheet(1);
//                    int rowsCount = excelWS.CountRows() - 1;

//                    if (rowsCount < 1)
//                        return MethodState.EMPTY_FILE;

//                    // Maps to the dictionary index -> columnName
//                    MapColumnsToDictionary(excelWS);

//                    using (SqlConnection sqlConnection = new SqlConnection(ConnectionString))
//                    {
//                        SqlTransaction sqlTransaction = null;

//                        try
//                        {
//                            sqlConnection.Open();
//                            sqlTransaction = sqlConnection.BeginTransaction();

//                            // for each row, create sql params with the name of the header and the value of the current cell
//                            for (int row = 1; row <= rowsCount; row++)
//                            {
//                                int adjustRow = row + 1;        // because the header doesn't count

//                                //
//                                // SQL

//                                SqlCommand cmd = BuildSqlCommand(sqlConnection, sqlTransaction, excelWS, adjustRow);

//                                if (cmd.ExecuteNonQuery() != 1)
//                                    throw new InvalidOperationException();
//                            }

//                            sqlTransaction.Commit();
//                        }

//                        catch (Exception)
//                        {
//                            if (sqlTransaction != null)
//                                sqlTransaction.Rollback();

//                            throw;
//                        }
//                    }
//                }


//                finally
//                {
//                    ForceCleanup(excelApp, excelWB, excelWS, processId);
//                }

//                return MethodState.OK;
//            }


//            // idxColumn, columnName, columnData, columnsLength; return overrided value
//            public event Func<int, string, string, int, string> Override;

//            // idxColumn, columnName, columnsLength; return overrided value
//            public event Func<int, string, int, string> OverrideHeader;



//            #endregion









//            #region Internal Methods

//            void MapColumnsToDictionary(Worksheet ws)
//            {
//                int columnsCount = ws.CountColumns();

//                for (int j = 1; j <= columnsCount; j++)
//                {
//                    // excel column name
//                    Range r = ws.GetCell(1, j);
//                    string columnName = r.Value2.ToString();
//                    Marshal.ReleaseComObject(r);

//                    if (OverrideHeader != null)
//                    {
//                        string overridedValue = OverrideHeader(j - 1, columnName, columnsCount);

//                        if (overridedValue != null)
//                            columnName = overridedValue;
//                    }

//                    m_parametersMapper[j] = columnName;
//                }
//            }



//            SqlCommand BuildSqlCommand(SqlConnection sqlConn, SqlTransaction sqlTransaction, Worksheet ws, int row)
//            {
//                SqlCommand sqlCmd = sqlConn.CreateCommand();

//                sqlCmd.CommandText = StoredProcedureName;
//                sqlCmd.CommandType = System.Data.CommandType.StoredProcedure;
//                sqlCmd.Transaction = sqlTransaction;

//                int columnsCount = ws.CountColumns();

//                for (int j = 1; j <= columnsCount; j++)
//                {
//                    // columnName
//                    string columnName = m_parametersMapper[j];

//                    // excel cell value
//                    string cellValue;
//                    Range r = ws.GetCell(row, j);
//                    cellValue = GetData(r);

//                    // Call override event, if defined..
//                    if (Override != null)
//                    {
//                        string overridedValue = Override(j - 1, columnName, cellValue, columnsCount);

//                        if (overridedValue != null)
//                            cellValue = overridedValue;
//                    }

//                    Marshal.ReleaseComObject(r);

//                    DbParameter p = new CustomDbParameter[] { new CustomDbParameter("@" + columnName, cellValue) }.ToDbParameters()[0];
//                    sqlCmd.Parameters.Add(p);
//                }

//                return sqlCmd;
//            }

//            static void ForceCleanup(Application excelApp, Workbook excelWB, Worksheet excelWS, uint processId)
//            {
//                if (excelWB != null)
//                    excelWB.Close();

//                if (excelApp != null)
//                    excelApp.Quit();

//                Marshal.ReleaseComObject(excelWS);
//                Marshal.ReleaseComObject(excelWB);
//                Marshal.ReleaseComObject(excelApp);

//                CleanProcess(processId);
//            }

//            static void CleanProcess(uint processId)
//            {
//                try
//                {
//                    Process.GetProcessesByName("EXCEL").Where(p => p.Id == processId).Single().Kill();
//                }
//                catch (Exception) { /* ignore  */ }
//            }

//            static String GetData(Range cell)
//            {
//                if (cell.Value != null && cell.Value.GetType() == typeof(DateTime))
//                    return cell.Value.ToString();

//                if (cell.Value2 == null)
//                    return "";

//                return cell.Value2.ToString();
//            }

//            static uint GetProcessIdFromHWnd(int hWnd)
//            {
//                return new IntPtr(hWnd).GetProcessId();
//            }


//            #endregion
//        }
//    }
//}