using HnPrinter.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HnPrinter.Models;
using HnPrinter.UseCases;

namespace HnPrinter
{
    public partial class frmMain : Form
    {
        ApiHelper _api;

        public frmMain()
        {
            InitializeComponent();
        }

        private Task _tQueueListener;
        private bool _runQueueListener = false;

        private void frmMain_Load(object sender, EventArgs e)
        {
            _api = new ApiHelper(System.Configuration.ConfigurationManager.AppSettings["ApiUrl"]);

            Start();
        }

        private void Start()
        {
            _runQueueListener = true;
            _tQueueListener = Task.Run(DoListen);
        }

        private void Stop()
        {
            _runQueueListener = false;

            try
            {
                if (_tQueueListener != null)
                    _tQueueListener.Dispose();
            }
            catch (Exception)
            {

            }
        }

        private void AddLog(string message)
        {
            try
            {
                this.BeginInvoke((Action)delegate
                {
                    if (lstLog.Items.Count > 100)
                        lstLog.Items.Clear();

                    lstLog.Items.Insert(0, string.Format("{0:[HH:mm]}", DateTime.Now) + ": " + message);
                });
            }
            catch (Exception)
            {

            }
        }

        private async Task DoListen()
        {
            while (_runQueueListener)
            {
                try
                {
                    var queue = await _api.GetData<PrintQueueModel[]>("Print");
                    if (queue != null && queue.Length > 0)
                    {
                        foreach (var printModel in queue)
                        {
                            try
                            {
                                var itemSerial = await _api.GetData<int>("Print/SerialNo/" + printModel.ItemId.ToString());
                                itemSerial++;

                                var itemData = await _api.GetData<ItemModel>("Item/" + printModel.ItemId.ToString());
                                if (itemData == null)
                                    continue;

                                printModel.ItemCode = itemData.ItemCode;

                                using (PrinterBO bObj = new PrinterBO())
                                {
                                    var printResult = bObj.PrintLabel(printModel, itemSerial);
                                    if (printResult)
                                    {
                                        printModel.IsPrinted = true;
                                        await _api.PostData<PrintQueueModel>("Print", printModel);
                                    }
                                }

                                AddLog(itemData.ItemCode + " yazdırıldı");

                                await Task.Delay(500);
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }
                catch (Exception)
                {

                }

                await Task.Delay(500);
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
