using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIExpansionKit.API;
using CSCore.CoreAudioAPI;
using MelonLoader;

namespace LocalAudioLinkTakeover
{
    public class DeviceListEntry
    {
        public string Name { get; set; }
        public MMDevice Device { get; set; }
    }

    public class MenuInteractionEventArgs : EventArgs
    {
        public enum MenuInteractionType
        {
            RELEASE_TAKEOVER,
            DISABLE_AUDIOLINK,
            SET_INPUT,
            SET_OUTPUT,
        };

        public MenuInteractionType Type { get; set; }

        public MMDevice deviceParam { get; set; }
    }

    internal class QuickMenuSettings
    {
        private MelonLogger.Instance LoggerInstance = new MelonLogger.Instance("Local_AudioLink_Takeover");

        public event EventHandler MenuInteractionEvent;

        private string currentDeviceName = "";
        private bool audioLinkFound = false;

        public void Init()
        {
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton("AudioLink Takeover", OpenMenu);
        }

        public void SetAudioLinkFound(bool found)
        {
            this.audioLinkFound = found;
        }

        private void OpenMenu()
        {
            var menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescriptionCustom.QuickMenu2Column);

            if (this.audioLinkFound)
            {
                menu.AddSimpleButton("Release Takeover", () =>
                {
                    var args = new MenuInteractionEventArgs { Type = MenuInteractionEventArgs.MenuInteractionType.RELEASE_TAKEOVER };

                    EventHandler handler = MenuInteractionEvent;
                    handler?.Invoke(this, args);

                    this.currentDeviceName = "";
                });

                menu.AddSimpleButton("Disable Audiolink", () =>
                {
                    var args = new MenuInteractionEventArgs { Type = MenuInteractionEventArgs.MenuInteractionType.DISABLE_AUDIOLINK };

                    EventHandler handler = MenuInteractionEvent;
                    handler?.Invoke(this, args);

                    this.currentDeviceName = "";
                });

                menu.AddLabel("Current Device:");
                menu.AddLabel(currentDeviceName);

                menu.AddLabel("Output devices:");
                menu.AddSpacer();

                var outputs = MMDeviceEnumerator.EnumerateDevices(DataFlow.Render);
                int addedButtons = 0;
                foreach (var output in outputs)
                {
                    string name;
                    DeviceState deviceState;
                    try
                    {
                        name = output.FriendlyName;
                        deviceState = output.DeviceState;

                        if (deviceState != DeviceState.Active)
                        {
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        continue; // ignore the unknown HRESULT error and skip the device
                    }

                    menu.AddSimpleButton(name, () =>
                    {
                        var args = new MenuInteractionEventArgs
                        {
                            Type = MenuInteractionEventArgs.MenuInteractionType.SET_OUTPUT,
                            deviceParam = output,
                        };

                        EventHandler handler = MenuInteractionEvent;
                        handler?.Invoke(this, args);

                        this.currentDeviceName = name;

                        // update menu
                        menu.Hide();
                        OpenMenu();
                    });

                    addedButtons++;
                }

                if (addedButtons % 2 == 1)
                {
                    menu.AddSpacer();
                }

                menu.AddLabel("Input devices:");
                menu.AddSpacer();

                var inputs = MMDeviceEnumerator.EnumerateDevices(DataFlow.Capture);
                addedButtons = 0;
                foreach (var input in inputs)
                {
                    string name;
                    DeviceState deviceState;
                    try
                    {
                        name = input.FriendlyName;
                        deviceState = input.DeviceState;

                        if (deviceState != DeviceState.Active)
                        {
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        continue; // ignore the unknown HRESULT error and skip the device
                    }

                    menu.AddSimpleButton(name, () =>
                    {
                        var args = new MenuInteractionEventArgs
                        {
                            Type = MenuInteractionEventArgs.MenuInteractionType.SET_INPUT,
                            deviceParam = input,
                        };

                        EventHandler handler = MenuInteractionEvent;
                        handler?.Invoke(this, args);

                        this.currentDeviceName = name;

                        // update menu
                        menu.Hide();
                        OpenMenu();
                    });

                    addedButtons++;
                }

                if (addedButtons % 2 == 1)
                {
                    menu.AddSpacer();
                }

                menu.AddSpacer();
                menu.AddSpacer();

                menu.AddSimpleButton("Refresh", () =>
                {
                    menu.Hide();
                    OpenMenu();
                });

                menu.AddSimpleButton("Close", () =>
                {
                    menu.Hide();
                });
            } 
            else
            {
                menu.AddLabel("AudioLink was not found in this world");

                menu.AddSimpleButton("Close", () =>
                {
                    menu.Hide();
                });
            }

            menu.Show();
        }
    }
}

namespace UIExpansionKit.API
{
    public struct LayoutDescriptionCustom
    {
        public static LayoutDescription QuickMenu2Column = new LayoutDescription { NumColumns = 2, RowHeight = 380 / 8, NumRows = 8 };
    }
}