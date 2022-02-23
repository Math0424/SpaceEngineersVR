using Sandbox;
using Sandbox.Graphics.GUI;
using System;
using System.Text;
using VRage;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineersVR.GUI
{
    public class MyPluginConfigDialog : MyGuiScreenBase
    {
        private const string Caption = "PluginTemplate Configuration";
        public override string GetFriendlyName()
        {
            return "MyPluginConfigDialog";
        }

        private MyLayoutTable layoutTable;

        private MyGuiControlLabel enabledLabel;
        private MyGuiControlCheckbox enabledCheckbox;

        // TODO: Add member variables for your UI controls here

        private MyGuiControlMultilineText infoText;
        private MyGuiControlButton closeButton;

        public MyPluginConfigDialog() : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.5f, 0.7f), false, null, MySandboxGame.Config.UIBkOpacity, MySandboxGame.Config.UIOpacity)
        {
            EnabledBackgroundFade = true;
            m_closeOnEsc = true;
            m_drawEvenWithoutFocus = true;
            CanHideOthers = true;
            CanBeHidden = true;
            CloseButtonEnabled = true;
        }

        public override void LoadContent()
        {
            base.LoadContent();
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            CreateControls();
            LayoutControls();
        }

        private void CreateControls()
        {
            AddCaption(Caption);

            Config.IPluginConfig config = Common.Common.Config;
            CreateCheckbox(out enabledLabel, out enabledCheckbox, config.Enabled, value => config.Enabled = value, "Enabled", "Enables the plugin");
            // TODO: Create your UI controls here

            infoText = new MyGuiControlMultilineText
            {
                Name = "InfoText",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                TextBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                // TODO: Add 2 short lines of text here if the player needs to know something. Ask for feedback here. Etc.
                Text = new StringBuilder("\r\nTODO")
            };

            closeButton = new MyGuiControlButton(originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER, text: MyTexts.Get(MyCommonTexts.Ok), onButtonClick: OnOk);
        }

        private void OnOk(MyGuiControlButton _)
        {
            CloseScreen();
        }

        private void CreateCheckbox(out MyGuiControlLabel labelControl, out MyGuiControlCheckbox checkboxControl, bool value, Action<bool> store, string label, string tooltip)
        {
            labelControl = new MyGuiControlLabel
            {
                Text = label,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            checkboxControl = new MyGuiControlCheckbox(toolTip: tooltip)
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                Enabled = true,
                IsChecked = value
            };
            checkboxControl.IsCheckedChanged += cb => store(cb.IsChecked);
        }

        private void LayoutControls()
        {
            Vector2 size = Size ?? Vector2.One;
            layoutTable = new MyLayoutTable(this, -0.3f * size, 0.6f * size);
            layoutTable.SetColumnWidths(400f, 100f);
            // TODO: Add more row heights here as needed
            layoutTable.SetRowHeights(90f, /* TODO */ 150f, 60f);

            int row = 0;

            layoutTable.Add(enabledLabel, MyAlignH.Left, MyAlignV.Center, row, 0);
            layoutTable.Add(enabledCheckbox, MyAlignH.Left, MyAlignV.Center, row, 1);
            row++;

            // TODO: Layout your UI controls here

            layoutTable.Add(infoText, MyAlignH.Left, MyAlignV.Top, row, 0, colSpan: 2);
            row++;

            layoutTable.Add(closeButton, MyAlignH.Center, MyAlignV.Center, row, 0, colSpan: 2);
            // row++;
        }
    }
}
