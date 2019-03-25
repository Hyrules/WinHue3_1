﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using WinHue3.Addons.CpuTempMon;
using WinHue3.Functions.Application_Settings.Settings;
using WinHue3.Functions.Grouping;
using WinHue3.Functions.HotKeys;
using WinHue3.Functions.PropertyGrid;
using WinHue3.Philips_Hue;
using WinHue3.Philips_Hue.BridgeObject;
using WinHue3.Philips_Hue.Communication;
using WinHue3.Philips_Hue.HueObjects.Common;
using WinHue3.Utils;
using HotKey = WinHue3.Functions.HotKeys.HotKey;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using WinHue3.ExtensionMethods;
using WinHue3.Functions.BridgeFinder;
using WinHue3.Functions.BridgeManager;
using WinHue3.Functions.RoomMap;
using WinHue3.Philips_Hue.BridgeObject.BridgeObjects;

namespace WinHue3.MainForm
{
    public partial class MainFormViewModel : ValidatableBindableBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private readonly DispatcherTimer _findlighttimer = new DispatcherTimer();
        private readonly DispatcherTimer _findsensortimer = new DispatcherTimer();
        private readonly List<HotKeyHandle> _lhk;
        private List<HotKey> _listHotKeys;
        private string _lastmessage = string.Empty;
        private MainFormModel _mainFormModel;
        private CpuTempMonitor _ctm;
        private readonly DispatcherTimer _ledTimer;
        private bool _hotkeyDetected;
        private TaskbarIcon _tbt;
        
        
        public MainFormViewModel()
        {

            _ledTimer = new DispatcherTimer()
            {
                Interval = new TimeSpan(0, 0, 0, 2)

            };
            Comm.CommunicationTimedOut += Comm_CommunicationTimedOut;
            _hotkeyDetected = false;
            _ledTimer.Tick += _ledTimer_Tick;
            _lhk = new List<HotKeyHandle>();
            
            _findlighttimer.Interval = new TimeSpan(0, 1, 0);
            _findlighttimer.Tick += _findlighttimer_Tick;
            _findsensortimer.Interval = new TimeSpan(0, 1, 0);
            _findsensortimer.Tick += _findsensortimer_Tick;


            _listHotKeys = WinHueSettings.hotkeys.listHotKeys;
            _mainFormModel = new MainFormModel();
            _sliderTT = WinHueSettings.settings.DefaultTT;
            Comm.Timeout = WinHueSettings.settings.Timeout;
            _mainFormModel.Sort = WinHueSettings.settings.Sort;
            _mainFormModel.ShowId = WinHueSettings.settings.ShowID;
            _mainFormModel.WrapText = WinHueSettings.settings.WrapText;
            _mainFormModel.Showhiddenscenes = WinHueSettings.settings.ShowHiddenScenes;
            _mainFormModel.ShowFloorPlanTab = WinHueSettings.settings.ShowFloorPlanTab;
            LoadFloorPlans();
        }



        public void LoadFloorPlans()
        {
            SelectedFloorPlan = null;
            ListFloorPlans = new ObservableCollection<Floor>(WinHueSettings.LoadFloorPlans());

        }

        public void SetToolbarTray(TaskbarIcon tbt)
        {
            _tbt = tbt;
        }

        private void Comm_CommunicationTimedOut(object sender, EventArgs e)
        {
            MessageBox.Show("Not Responding");
        }

        private void _ledTimer_Tick(object sender, EventArgs e)
        {
            _ledTimer.Stop();
            HotkeyDetected = false;
        }

        public MainFormModel MainFormModel
        {
            get => _mainFormModel;
            set => SetProperty(ref _mainFormModel, value);
        }

        public bool HotkeyDetected
        {
            get => _hotkeyDetected;
            set => SetProperty(ref _hotkeyDetected, value);
        }

        private void Initialize()
        {
            UpdateManager.CheckForWinHueUpdate();

            if (UpdateManager.UpdateAvailable)
            {
                RaisePropertyChanged("AppUpdateAvailable");
                if (WinHueSettings.settings.CheckForUpdate)
                {

                    if (MessageBox.Show(GlobalStrings.UpdateAvailableDownload, GlobalStrings.Warning, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        UpdateManager.DownloadUpdate();
                    }

                }
            }

            BridgeManager.Instance.OnBridgeMessageAdded += Bridge_OnMessageAdded;
            BridgeManager.Instance.OnBridgeNotResponding += Bridge_BridgeNotResponding;
            BridgeManager.Instance.LoadBridges();  
            
            if (BridgeManager.Instance.SelectedBridge != null)
                _ctm = new CpuTempMonitor(BridgeManager.Instance.SelectedBridge);
            
        }



    }
}

