/////////////////////////////////////////////
// Jim Hollenhorst, February 4, 2006
// www.ultrapico.com
// Feel free to duplicate and distribute
// Kudos to Rick Brewster and JasonD
/////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace ImagePro
{
    //
    //对ToolStrip和MenuStrip失去焦点时行为的改进
    //拥有两个属性，clickThrough失去焦点时点击也可响应，suppressHighlighting失去焦点时鼠标移入高亮
    //
    // The ToolStrip and MenuStrip classes are extended to allow customization of the user interface
    //
    // The following new boolean properties are exposed in the designer:
    //
    // ClickThrough - Allow the first click to activate the control, even when the containing form is not active
    // SupressHighlighting - Suppress the mouseover highlighting of the control when the containing form is not active
    //
    // The ideas behind this were borrowed from two items found on the Internet:
    //
    // Rick Brewster shows how to implement ClickThrough on his blog at:
    //   http://blogs.msdn.com/rickbrew/
    //
    // JasonD suggests the method to suppress the highlighting on at forum at: 
    //   http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=118385&SiteID=1
    //

    /// <summary>
    /// Define some Windows message constants
    /// </summary>
    public class WinConst
    {
        public const uint WM_MOUSEMOVE = 0x0200;
        public const uint WM_MOUSEACTIVATE = 0x21;
        public const uint MA_ACTIVATE = 1;
        public const uint MA_ACTIVATEANDEAT = 2;
        public const uint MA_NOACTIVATE = 3;
        public const uint MA_NOACTIVATEANDEAT = 4;
    }

    /// <summary>
    /// This class adds to the functionality provided in System.Windows.Forms.MenuStrip.
    /// 
    /// It allows you to "ClickThrough" to the MenuStrip so that you don't have to click once to 
    /// bring the form into focus and once more to take the desired action
    /// 
    /// It also implements a SuppressHighlighting property to turn off the highlighting
    /// that occures on mouseover when the form is not active
    /// </summary>
    public class MenuStripEx : MenuStrip
    {
        private bool clickThrough = true;
        private bool suppressHighlighting = false;

        /// <summary>
        /// Gets or sets whether the control honors item clicks when its containing form does
        /// not have input focus.
        /// </summary>
        /// <remarks>
        /// Default value is false, which is the same behavior provided by the base ToolStrip class.
        /// </remarks>
        public bool ClickThrough
        {
            get { return this.clickThrough; }
            set { this.clickThrough = value; }
        }

        /// <summary>
        /// Gets or sets whether the control shows highlighting on mouseover
        /// </summary>
        /// <remarks>
        /// Default value is true, which is the same behavior provided by the base MenuStrip class.
        /// </remarks>
        public bool SuppressHighlighting
        {
            get { return this.suppressHighlighting; }
            set { this.suppressHighlighting = value; }
        }

        /// <summary>
        /// This method overrides the procedure that responds to Windows messages.
        /// 
        /// It intercepts the WM_MOUSEMOVE message
        /// and ignores it if SuppressHighlighting is on and the TopLevelControl does not contain the focus.
        /// Otherwise, it calls the base class procedure to handle the message.
        /// 
        /// It also intercepts the WM_MOUSEACTIVATE message and replaces an "Activate and Eat" result with
        /// an "Activate" result if ClickThrough is enabled.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            // If we don't want highlighting, throw away mousemove commands
            // when the parent form or one of its children does not have the focus
            if (m.Msg == WinConst.WM_MOUSEMOVE && this.suppressHighlighting && !this.TopLevelControl.ContainsFocus)
                return;
            else
                base.WndProc(ref m);

            // If we want ClickThrough, replace "Activate and Eat" with "Activate" on WM_MOUSEACTIVATE messages
            if (m.Msg == WinConst.WM_MOUSEACTIVATE && this.clickThrough && m.Result == (IntPtr)WinConst.MA_ACTIVATEANDEAT)
                m.Result = (IntPtr)WinConst.MA_ACTIVATE;
        }
    }

    /// <summary>
    /// This class adds to the functionality provided in System.Windows.Forms.ToolStrip.
    /// 
    /// It allows you to "ClickThrough" to the MenuStrip so that you don't have to click once to 
    /// bring the form into focus and once more to take the desired action
    /// 
    /// It also implements a SuppressHighlighting property to turn off the highlighting
    /// that occures on mouseover when the form is not active
    /// </summary>
    public class ToolStripEx : ToolStrip
    {
        private bool clickThrough = true;
        private bool suppressHighlighting = false;

        /// <summary>
        /// Gets or sets whether the control honors item clicks when its containing form does
        /// not have input focus.
        /// </summary>
        /// <remarks>
        /// Default value is false, which is the same behavior provided by the base ToolStrip class.
        /// </remarks>
        public bool ClickThrough
        {
            get { return this.clickThrough; }
            set { this.clickThrough = value; }
        }

        /// <summary>
        /// Gets or sets whether the control shows highlighting on mouseover
        /// </summary>
        /// <remarks>
        /// Default value is true, which is the same behavior provided by the base MenuStrip class.
        /// </remarks>
        public bool SuppressHighlighting
        {
            get { return this.suppressHighlighting; }
            set { this.suppressHighlighting = value; }
        }

        /// <summary>
        /// This method overrides the procedure that responds to Windows messages.
        /// 
        /// It intercepts the WM_MOUSEMOVE message
        /// and ignores it if SuppressHighlighting is on and the TopLevelControl does not contain the focus.
        /// Otherwise, it calls the base class procedure to handle the message.
        /// 
        /// It also intercepts the WM_MOUSEACTIVATE message and replaces an "Activate and Eat" result with
        /// an "Activate" result if ClickThrough is enabled.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            // If we don't want highlighting, throw away mousemove commands
            // when the parent form or one of its children does not have the focus
            if (m.Msg == WinConst.WM_MOUSEMOVE && this.suppressHighlighting && !this.TopLevelControl.ContainsFocus)
                return;
            else
                base.WndProc(ref m);

            // If we want ClickThrough, replace "Activate and Eat" with "Activate" on WM_MOUSEACTIVATE messages
            if (m.Msg == WinConst.WM_MOUSEACTIVATE && this.clickThrough && m.Result == (IntPtr)WinConst.MA_ACTIVATEANDEAT)
                m.Result = (IntPtr)WinConst.MA_ACTIVATE;
        }
    }
}
