﻿using System;
using System.IO;
using Eto.Drawing;
using Eto.Forms;
using log4net;
using Titan.Core;
using Titan.UI.Commands.Links;

namespace Titan.UI
{
    public sealed class MainForm : Form
    {

        private readonly string _icon = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "Resources" +
                                        Path.DirectorySeparatorChar + "Logo.ico";

        public readonly ILog Log = LogManager.GetLogger(typeof(MainForm));

        private readonly TextBox _targetBox;
        private readonly TextBox _matchIDBox;

        public MainForm()
        {
            Title = "Titan";
            ClientSize = new Size(600, 160);
            Resizable = false;
            Icon = new Icon(File.Open(_icon, FileMode.Open));

            // Selected arguments for the UI
            _targetBox = new TextBox { PlaceholderText = "76561198224231904" };
            _matchIDBox = new TextBox { PlaceholderText = "3203363151840018511" };
            var bombBtn = new Button { Text = "Bomb!" };
            bombBtn.Click += OnBombButtonClick;

            Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Padding = new Padding(10, 10, 10, 10),
                Rows =
                {
                    new TableRow(
                        new TableCell(new Label { Text = "Mode" }, true),
                        new TableCell(new DropDown { Items = { "Report"/*, "Commend"*/ }, SelectedIndex = 0 }, true)
                    ),
                    new TableRow(
                        new Label { Text = "Target" },
                        _targetBox
                    ),
                    new TableRow(
                        new Label { Text = "Match ID" },
                        _matchIDBox
                    ),
                    new TableRow(new TableCell(), new TableCell()),
                    new TableRow(new TableCell(), new TableCell()),
                    new TableRow(new TableCell(), new TableCell()),
                    new TableRow(
                        new TableCell(),
                        bombBtn
                    ),
                    new TableRow { ScaleHeight = true }
                }
            };

            Menu = new MenuBar
            {
                Items =
                {
                    new ButtonMenuItem { Text = "&Links", Items = {
                        new SteamIO(),
                        new JsonValidator()
                    }}
                },
                AboutItem = new Commands.About(),
                QuitItem = new Commands.Quit()
            };

        }

        public void OnBombButtonClick(object sender, EventArgs args)
        {
            if(!string.IsNullOrWhiteSpace(_targetBox.Text) || !string.IsNullOrEmpty(_matchIDBox.Text))
            {
                Log.InfoFormat("Bomb! Button has been pressed. Starting bombing to {0} in match {1}.", _targetBox.Text, _matchIDBox.Text);

                // Hub.StartBotting(_targetBox.Text, _matchIDBox.Text);
            }
            else
            {
                MessageBox.Show("Please provide the Target and the Match ID.", "Error - Titan", MessageBoxType.Error);
            }
        }

    }
}