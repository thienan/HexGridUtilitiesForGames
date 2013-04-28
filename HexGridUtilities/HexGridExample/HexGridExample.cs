﻿#region License - Copyright (C) 2012-2013 Pieter Geerkens, all rights reserved.
/////////////////////////////////////////////////////////////////////////////////////////
//                PG Software Solutions Inc. - Hex-Grid Utilities
//
// Use of this software is permitted only as described in the attached file: license.txt.
/////////////////////////////////////////////////////////////////////////////////////////
#endregion
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using PG_Napoleonics.Utilities;
using PG_Napoleonics.Utilities.HexUtilities;
using PG_Napoleonics.Utilities.WinForms;

namespace PG_Napoleonics.HexGridExample {
  public sealed partial class HexGridExampleForm : Form, IMessageFilter {
    public HexGridExampleForm() {
      InitializeComponent();

      hexgridPanel.Host = 
      MapBoard          = new TerrainMap();
      MapBoard.ShowFov  = buttonFieldOfView.Checked
                        = true;
      toolStripComboBox1.SelectedIndex = 0;
      var matrix        = new IntMatrix2D(2,0, 0,-2, 0,2*MapBoard.SizeHexes.Height-1, 2);
      HexCoords.SetCustomMatrices(matrix,matrix);

			Application.AddMessageFilter(this);
    }
    protected override CreateParams CreateParams { 
			get { return this.SetCompositedStyle(base.CreateParams); }
		}

    MapDisplay MapBoard     { get; set; }

    #region Event handlers
    void HexGridExampleForm_Load(object sender, EventArgs e) {
      hexgridPanel.ScaleIndex = 1; 

      Size = hexgridPanel.MapSizePixels + new Size(21,93);
    }

    bool isPanelResizeSuppressed = false;
    protected override void OnResizeBegin(EventArgs e) {
      base.OnResizeBegin(e);
      isPanelResizeSuppressed = true;
    }
    protected override void OnResize(EventArgs e) {
      base.OnResize(e);
      if (IsHandleCreated && ! isPanelResizeSuppressed) hexgridPanel.SetScroll();
    }
    protected override void OnResizeEnd(EventArgs e) {
      base.OnResizeEnd(e);
      isPanelResizeSuppressed = false;
      hexgridPanel.SetScroll();
    }

    private void hexgridPanel_MouseClick() {
      MapBoard.Path = PathFinder.FindPath(
        MapBoard.StartHex.Canon, 
        MapBoard.GoalHex.Canon, 
        (c,hs) => MapBoard.StepCost(c,hs),
        c => MapBoard.Range(MapBoard.HotSpotHex.Canon,c),
        c => MapBoard.IsOnBoard(c.User)
      );
    }
    void hexgridPanel_MouseMove(object sender, MouseEventArgs e) {
      var hotHex       = MapBoard.HotSpotHex;
      statusLabel.Text = "HotHex: " + hotHex.ToString() 
                       + "/" + hotHex.Canon.Custom.ToString()
                       + "/" + hotHex.Canon.ToString()
                       + "; Range = " + MapBoard.StartHex.Range(hotHex)
                       + "; Path Length = " + (MapBoard.Path==null ? 0 : MapBoard.Path.TotalCost);
    }

    void buttonTransposeMap_Click(object sender, EventArgs e) {
      hexgridPanel.IsTransposed = buttonTransposeMap.Checked;
    }
    #endregion

    private void toolStripComboBox1_Click(object sender, EventArgs e) {
      var name = ((ToolStripItem)sender).Text;
      switch (name) {
        case "MazeMap":    hexgridPanel.Host = MapBoard = new MazeMap();    break;
        case "TerrainMap": hexgridPanel.Host = MapBoard = new TerrainMap(); break;
        default:  break;
      }
      MapBoard.ShowFov = buttonFieldOfView.Checked;
      hexgridPanel.Refresh();
    }

    private void buttonFieldOfView_Click(object sender, EventArgs e) {
      MapBoard.ShowFov = buttonFieldOfView.Checked;
    }

    private void PanelBoard_GoalHexChange(object sender, HexEventArgs e) {
      MapBoard.GoalHex = e.Coords;
      hexgridPanel_MouseClick();
      Refresh();
    }
    private void PanelBoard_StartHexChange(object sender, HexEventArgs e) {
      MapBoard.StartHex = e.Coords;
      hexgridPanel_MouseClick();
      Refresh();
    }
    private void PanelBoard_HotSpotHexChange(object sender, HexEventArgs e) {
      MapBoard.HotSpotHex = e.Coords;
      Refresh();
    }

    #region IMessageFilter implementation
    /// <summary>Redirect WM_MouseWheel messages to window under mouse.</summary>
		/// <remarks>Redirect WM_MouseWheel messages to window under mouse (rather than 
    /// that with focus) with adjusted delta.
    /// <see cref="http://www.flounder.com/virtual_screen_coordinates.htm"/>
    /// Dont forget to add this to constructor:
    /// 			Application.AddMessageFilter(this);
    ///</remarks>
		/// <param name="m">The Windows Message to filter and/or process.</param>
		/// <returns>Success (true) or failure (false) to OS.</returns>
		[System.Security.Permissions.PermissionSetAttribute(
			System.Security.Permissions.SecurityAction.Demand, Name="FullTrust")]
		bool IMessageFilter.PreFilterMessage(ref Message m) {
			var hWnd  = WindowFromPoint( WindowsMouseInput.GetPointLParam(m.LParam) );
			var ctl	  = Control.FromHandle(hWnd);
      if (hWnd != IntPtr.Zero  &&  hWnd != m.HWnd  &&  ctl != null) {
        switch((WM)m.Msg) {
          default:  break;
          case WM.MOUSEWHEEL:
            #if DEBUG
              DebugTracing.Trace(TraceFlag.ScrollEvents, true," - {0}.WM.{1}: ", Name, ((WM)m.Msg)); 
            #endif
            if (ctl is HexgridPanel  ||  ctl is HexGridExampleForm) {
              return (SendMessage(hWnd, m.Msg, m.WParam, m.LParam) == IntPtr.Zero);
            }
            break;
        }
      }
      return false;
		}
    #region Extern declarations
    /// <summary>P/Invoke declaration for user32.dll.WindowFromPoint</summary>
		/// <remarks><see cref="http://msdn.microsoft.com/en-us/library/windows/desktop/ms633558(v=vs.85).aspx"/></remarks>
		/// <param name="pt">(Sign-extended) screen coordinates as a Point structure.</param>
		/// <returns>Window handle (hWnd).</returns>
		[DllImport("user32.dll")]
		private static extern IntPtr WindowFromPoint(Point pt);
		/// <summary>P/Invoke declaration for user32.dll.SendMessage</summary>
		/// <param name="hWnd">Window handle</param>
		/// <param name="msg">Windows message</param>
		/// <param name="wp">WParam</param>
		/// <param name="lp">LParam</param>
		/// <returns></returns>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    #endregion
    #endregion
  }
}