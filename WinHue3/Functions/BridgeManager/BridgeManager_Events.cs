﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinHue3.ExtensionMethods;
using WinHue3.Philips_Hue.BridgeObject;
using WinHue3.Philips_Hue.BridgeObject.BridgeMessages;
using WinHue3.Philips_Hue.HueObjects.Common;

namespace WinHue3.Functions.BridgeManager
{
    public sealed partial class BridgeManager
    {
        #region EVENTS
        public event BridgeRemoved OnBridgeRemoved;
        public delegate void BridgeRemoved(Bridge b);

        public event BridgeAdded OnBridgeAdded;
        public delegate void BridgeAdded(Bridge b);

        public event BridgeNotResponding OnBridgeNotResponding;
        public delegate void BridgeNotResponding(object sender, BridgeNotRespondingEventArgs e);

        public event BridgeAddedMessage OnBridgeMessageAdded;
        public delegate void BridgeAddedMessage(object sender, MessageAddedEventArgs e);

        private async void _refreshTimer_Tick(object sender, EventArgs e)
        {
            IHueObject selected = SelectedObject;
            List<IHueObject> obj = await SelectedBridge.GetAllObjectsAsync(false, true);

            List<IHueObject> diff = obj.Where(x => !_listCurrentBridgeHueObjects.Any(y => y.Id == x.Id && y.GetType() == x.GetType())).ToList();

            foreach (IHueObject ho in diff)
            {
                CurrentBridgeHueObjectsList.Remove(x => x.Id == ho.Id && x.GetType() == ho.GetType());
            }

            foreach (IHueObject o in obj)
            {
                IHueObject oldo = _listCurrentBridgeHueObjects.FirstOrDefault(x => x.Id == o.Id && x.GetType() == o.GetType());
                if (oldo == null)
                {
                    _listCurrentBridgeHueObjects.Add(o);
                }
                else
                {
                    RefreshHueObject(ref oldo, o);
                }

            }

            if (SelectedObject is null) return;
            if (obj.Any(x => x.Id == selected.Id && x.GetType() == selected.GetType()))
            {
                SelectedObject = selected;
            }
        }

        private void Br_BridgeNotResponding(object sender, BridgeNotRespondingEventArgs e)
        {
            OnBridgeNotResponding?.Invoke(sender, e);
        }

        private void LastCommandMessages_OnMessageAdded(object sender, MessageAddedEventArgs e)
        {
            OnBridgeMessageAdded?.Invoke(sender, e);
        }

        #endregion
    }
}
