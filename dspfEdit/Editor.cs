﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace dspfEdit
{
    public partial class Editor : Form
    {
        public static Size WindowSize;
        private int fieldCounter = 0;
        private string _File;

        public Editor(Dictionary<String, RecordInfo> Formats = null, string LocalFile = "")
        {
            _File = LocalFile;

            InitializeComponent();

            field_name.TextChanged += field_save_Click;
            field_val.TextChanged += field_save_Click;

            field_input.CheckedChanged += field_save_Click;
            field_output.CheckedChanged += field_save_Click;
            field_text.CheckedChanged += field_save_Click;
            field_hidden.CheckedChanged += field_save_Click;
            field_both.CheckedChanged += field_save_Click;
            field_number.CheckedChanged += field_save_Click;

            field_len.ValueChanged += field_save_Click;
            field_dec.ValueChanged += field_save_Click;

            field_colour.SelectedIndexChanged += field_save_Click;

            field_x.ValueChanged += field_save_Click;
            field_y.ValueChanged += field_save_Click;

            Boolean loadNew = false;
            if (Formats == null)
            {
                loadNew = true;
            }
            else
            {
                if (Formats.Count == 0)
                {
                    loadNew = true;
                }
                else
                {
                    tabControl1.TabPages.Clear();
                    foreach (string Format in Formats.Keys)
                    {
                        tabControl1.TabPages.Add(Format);
                    }
                    rcd_name.Text = Formats.Keys.ToArray()[0];
                    tabControl1.SelectedIndex = 0;

                    RecordFormats = Formats;
                    LoadFormat(rcd_name.Text);
                }
            }

            if (loadNew)
            {
                this.CurrentRecordFormat = "NEWFMT";
                tabControl1.TabPages.Add(new TabPage(this.CurrentRecordFormat));
                rcd_name.Text = this.CurrentRecordFormat;
                LoadFormat(this.CurrentRecordFormat);
            }
        }

        #region onClicks

        private Control CurrentlySelectedField;
        private void label_MouseUp(object sender, MouseEventArgs e)
        {
            Control controlItem = (Control)sender;
            FieldInfo fieldInfo = (FieldInfo)controlItem.Tag;

            field_x.ValueChanged -= field_save_Click;
            field_y.ValueChanged -= field_save_Click;

            field_x.Value = fieldInfo.Position.X;
            field_y.Value = fieldInfo.Position.Y;

            field_x.ValueChanged += field_save_Click;
            field_y.ValueChanged += field_save_Click;
        }
        private void label_MouseClick(object sender, MouseEventArgs e)
        {
            if (CurrentlySelectedField != null)
                CurrentlySelectedField.BackColor = Color.Black;

            CurrentlySelectedField = null;

            Control controlItem = (Control)sender;
            FieldInfo fieldInfo = (FieldInfo)controlItem.Tag;

            groupBox1.Visible = true;
            groupBox1.Text = fieldInfo.Name;

            field_name.Text = fieldInfo.Name;
            field_val.Text = fieldInfo.Value;

            field_input.Checked = fieldInfo.fieldType == FieldInfo.FieldType.Input;
            field_output.Checked = fieldInfo.fieldType == FieldInfo.FieldType.Output;
            field_text.Checked = fieldInfo.fieldType == FieldInfo.FieldType.Const;
            field_both.Checked = fieldInfo.fieldType == FieldInfo.FieldType.Both;
            field_hidden.Checked = fieldInfo.fieldType == FieldInfo.FieldType.Hidden;

            field_number.Checked = (fieldInfo.dataType == FieldInfo.DataType.Decimal);
            field_dec.Enabled = (fieldInfo.dataType == FieldInfo.DataType.Decimal);
            field_dec.Value = (field_dec.Enabled ? fieldInfo.Decimals : 0);

            field_len.Enabled = !field_text.Checked;
            field_len.Value = fieldInfo.Length;

            field_colour.SelectedIndex = field_colour.Items.IndexOf(fieldInfo.Colour);

            field_x.Enabled = (fieldInfo.fieldType != FieldInfo.FieldType.Hidden);
            field_y.Enabled = (fieldInfo.fieldType != FieldInfo.FieldType.Hidden);

            if (fieldInfo.fieldType != FieldInfo.FieldType.Hidden)
            {
                field_x.Value = fieldInfo.Position.X;
                field_y.Value = fieldInfo.Position.Y;
            }

            int index = comboBox1.Items.IndexOf(fieldInfo.Name);
            if (index >= 0)
                comboBox1.SelectedIndex = index;

            controlItem.BackColor = Color.DimGray;
            controlItem.BringToFront();

            this.CurrentlySelectedField = controlItem;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int index = comboBox1.Items.IndexOf(CurrentlySelectedField.Name);
            if (index >= 0)
                comboBox1.Items.RemoveAt(index);

            CurrentlySelectedField.Dispose();
            groupBox1.Visible = false;
        }

        private void screen_MouseClick(object sender, MouseEventArgs e)
        {
            if (CurrentlySelectedField != null)
                CurrentlySelectedField.BackColor = Color.Black;

            comboBox1.SelectedIndex = -1;
            CurrentlySelectedField = null;
            groupBox1.Visible = false;
        }

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            int index = comboBox1.SelectedIndex;
            if (index >= 0)
            {
                if (screen.Controls[comboBox1.Items[index].ToString()] != null)
                {
                    if (CurrentlySelectedField != null)
                    {
                        if (CurrentlySelectedField.Name != comboBox1.Items[index].ToString())
                        {
                            label_MouseClick(screen.Controls[comboBox1.Items[index].ToString()], null);
                        }
                    }
                    else
                    {
                        label_MouseClick(screen.Controls[comboBox1.Items[index].ToString()], null);
                    }
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFormat(rcd_name.Text);
            if (_File != "")
            {
                DisplayGenerate dspfGen = new DisplayGenerate();
                dspfGen.Generate(RecordFormats);
                File.WriteAllLines(_File, dspfGen.GetOutput());
            }
        }

        #endregion

        public static Point DSPFtoUILoc(Point point)
        {
            int x = point.X - 1, y = point.Y - 1;

            x = x * 9;
            y = y * 19;

            return new Point(x, y);
        }

        #region LabelAdding
        public void AddLabel(FieldInfo.DataType Type, Point location, FieldInfo PreInfo = null)
        {
            DragLabel text = new DragLabel();
            FieldInfo fieldInfo;

            if (PreInfo == null)
            {
                fieldCounter++;
                fieldInfo = new FieldInfo();
                fieldInfo.Name = Type.ToString().ToUpper() + fieldCounter.ToString();
                fieldInfo.Length = Type.ToString().Length;
                fieldInfo.Value = Type.ToString();
                fieldInfo.Position = location;

                fieldInfo.dataType = Type;
                fieldInfo.fieldType = FieldInfo.FieldType.Both;
            }
            else
            {
                fieldInfo = PreInfo;
            }

            text.AutoSize = true;
            text.Name = fieldInfo.Name;
            text.Text = fieldInfo.Value;
            text.Tag = fieldInfo;
            text.Location = DSPFtoUILoc(fieldInfo.Position);
            text.Visible = (fieldInfo.fieldType != FieldInfo.FieldType.Hidden);

            text.ForeColor = FieldInfo.TextToColor(fieldInfo.Colour);
            if (fieldInfo.Value.Trim() == "")
            {
                text.Text = fieldInfo.Value.PadRight(fieldInfo.Length, '_');
            }
            else
            {
                text.Text = fieldInfo.Value.PadRight(fieldInfo.Length);
            }

            if (fieldInfo.fieldType == FieldInfo.FieldType.Input)
            {
                text.Font = new Font(screen.Font, FontStyle.Underline);
            }
            else
            {
                text.Font = new Font(screen.Font, FontStyle.Regular);
            }

            text.MouseClick += label_MouseClick;
            text.MouseUp += label_MouseUp;

            screen.Controls.Add(text);
            comboBox1.Items.Add(fieldInfo.Name);
        }

        private void textToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddLabel(FieldInfo.DataType.Char, new Point(1, 1));
        }
        #endregion

        public void field_save_Click(object sender, EventArgs e)
        {
            if (CurrentlySelectedField == null) return;
            FieldInfo fieldInfo = (FieldInfo)CurrentlySelectedField.Tag;

            if (field_name.Text.Trim() == "") field_name.Text = "FIELD";

            fieldInfo.Name = field_name.Text;
            fieldInfo.Length = Convert.ToInt32(field_len.Value);
            fieldInfo.Value = field_val.Text;
            fieldInfo.Position = new Point(Convert.ToInt32(field_x.Value), Convert.ToInt32(field_y.Value));
            fieldInfo.Decimals = Convert.ToInt32(field_dec.Value);

            if (field_text.Checked)
                fieldInfo.fieldType = FieldInfo.FieldType.Const;
            if (field_input.Checked)
                fieldInfo.fieldType = FieldInfo.FieldType.Input;
            if (field_output.Checked)
                fieldInfo.fieldType = FieldInfo.FieldType.Output;
            if (field_both.Checked)
                fieldInfo.fieldType = FieldInfo.FieldType.Both;
            if (field_hidden.Checked)
                fieldInfo.fieldType = FieldInfo.FieldType.Hidden;

            if (field_number.Checked)
                fieldInfo.dataType = FieldInfo.DataType.Decimal;
            else
                fieldInfo.dataType = FieldInfo.DataType.Char;

            field_len.Enabled = (fieldInfo.fieldType != FieldInfo.FieldType.Const);
            field_dec.Enabled = (field_len.Enabled && field_number.Checked);

            field_x.Enabled = (fieldInfo.fieldType != FieldInfo.FieldType.Hidden);
            field_y.Enabled = (fieldInfo.fieldType != FieldInfo.FieldType.Hidden);

            if (fieldInfo.fieldType == FieldInfo.FieldType.Const)
            {
                if (fieldInfo.Value.Length == 0) fieldInfo.Value = "-";

                fieldInfo.Length = fieldInfo.Value.Length;
                field_len.Value = fieldInfo.Length;
            }
            else
            {
                if (fieldInfo.Value.Length > fieldInfo.Length)
                {
                    fieldInfo.Value = fieldInfo.Value.Substring(0, fieldInfo.Length);
                    field_val.Text = fieldInfo.Value;
                }
            }

            if (field_colour.SelectedIndex >= 0)
            {
                fieldInfo.Colour = field_colour.SelectedItems[0].ToString();
            }

            int index = comboBox1.FindStringExact(CurrentlySelectedField.Name);
            if (index >= 0)
                comboBox1.Items[index] = fieldInfo.Name;

            field_x.Maximum = WindowSize.Width - fieldInfo.Length;
            field_y.Maximum = WindowSize.Height;

            CurrentlySelectedField.Name = fieldInfo.Name;
            CurrentlySelectedField.Location = DSPFtoUILoc(fieldInfo.Position);
            CurrentlySelectedField.ForeColor = FieldInfo.TextToColor(fieldInfo.Colour);
            CurrentlySelectedField.Visible = (fieldInfo.fieldType != FieldInfo.FieldType.Hidden);
            if (fieldInfo.Value.Trim() == "")
            {
                CurrentlySelectedField.Text = fieldInfo.Value.PadRight(fieldInfo.Length, '_');
            }
            else
            {
                CurrentlySelectedField.Text = fieldInfo.Value.PadRight(fieldInfo.Length);
            }
            if (fieldInfo.fieldType == FieldInfo.FieldType.Input || fieldInfo.fieldType == FieldInfo.FieldType.Both)
            {
                CurrentlySelectedField.Font = new Font(CurrentlySelectedField.Font, FontStyle.Underline);
            }
            else
            {
                CurrentlySelectedField.Font = new Font(CurrentlySelectedField.Font, FontStyle.Regular);
            }
        }

        #region Record Formats
        private Dictionary<string, RecordInfo> RecordFormats = new Dictionary<string, RecordInfo>();
        private string CurrentRecordFormat = "";

        private void recordFormatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tabControl1.TabPages.Add(new TabPage("NEWFMT"));
        }

        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            //Saving old tab
            TabPage current = (sender as TabControl).SelectedTab;
            string RcdFmtName = this.CurrentRecordFormat;

            SaveFormat(RcdFmtName);

            screen.Controls.Clear();
        }

        private void tabControl1_TabIndexChanged(object sender, TabControlEventArgs e)
        {
            //Loading new tab
            groupBox1.Visible = false;
            string RcdFmtName = tabControl1.SelectedTab.Text;
            LoadFormat(RcdFmtName);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Handle rename
            if (this.CurrentRecordFormat != rcd_name.Text)
            {
                if (RecordFormats.ContainsKey(this.CurrentRecordFormat))
                {
                    RecordFormats.Remove(this.CurrentRecordFormat);
                }
                tabControl1.SelectedTab.Text = rcd_name.Text;
            }

            SaveFormat(rcd_name.Text);

            this.CurrentRecordFormat = rcd_name.Text;
        }

        private void LoadFormat(String RcdFmtName)
        {
            this.CurrentRecordFormat = RcdFmtName;

            rcd_name.Text = this.CurrentRecordFormat;

            if (!RecordFormats.ContainsKey(RcdFmtName))
                RecordFormats.Add(RcdFmtName, new RecordInfo(RcdFmtName));

            comboBox1.Items.Clear();
            foreach (FieldInfo field in RecordFormats[RcdFmtName].Fields)
            {
                AddLabel(field.dataType, field.Position, field);
            }

            for (int i = 0; i < 24; i++)
            {
                rec_funcs.SetItemChecked(i, RecordFormats[RcdFmtName].FunctionKeys[i]);
            }

            rec_window.Checked = RecordFormats[RcdFmtName].isWindow;

            rec_sizex.Enabled = RecordFormats[RcdFmtName].isWindow;
            rec_sizey.Enabled = RecordFormats[RcdFmtName].isWindow;

            if (RecordFormats[RcdFmtName].isWindow)
            {
                rec_sizex.Value = RecordFormats[RcdFmtName].WindowSize.Width;
                rec_sizey.Value = RecordFormats[RcdFmtName].WindowSize.Height;
            }

            AdjustScreenSize();
        }

        private void SaveFormat(string RcdFmtName)
        {
            List<FieldInfo> RecordFields = new List<FieldInfo>();

            foreach (Control field in screen.Controls)
            {
                if (field.Tag is FieldInfo)
                {
                    RecordFields.Add(field.Tag as FieldInfo);
                }
            }

            if (!RecordFormats.ContainsKey(RcdFmtName))
                RecordFormats.Add(RcdFmtName, new RecordInfo(RcdFmtName));

            RecordFormats[RcdFmtName].Fields = RecordFields.ToArray();

            for (int i = 0; i < 24; i++)
            {
                RecordFormats[RcdFmtName].FunctionKeys[i] = rec_funcs.GetItemChecked(i);
            }

            RecordFormats[RcdFmtName].Pagedown = rec_pagedown.Checked;
            RecordFormats[RcdFmtName].Pageup = rec_pageup.Checked;

            RecordFormats[RcdFmtName].isWindow = rec_window.Checked;
            RecordFormats[RcdFmtName].WindowSize = new Size(Convert.ToInt32(rec_sizex.Value), Convert.ToInt32(rec_sizey.Value));

            RecordFormats[RcdFmtName].Name = RcdFmtName;

            rec_sizex.Enabled = RecordFormats[RcdFmtName].isWindow;
            rec_sizey.Enabled = RecordFormats[RcdFmtName].isWindow;

            AdjustScreenSize();
        }

        private void AdjustScreenSize()
        {
            Boolean normalSize = false;
            if (this.CurrentRecordFormat != null)
            {
                if (RecordFormats.ContainsKey(this.CurrentRecordFormat))
                {
                    if (RecordFormats[this.CurrentRecordFormat].isWindow)
                    {
                        Size windowSize = RecordFormats[this.CurrentRecordFormat].WindowSize;
                        screen.Size = new Size(DSPFtoUILoc(new Point(windowSize.Width + 2, windowSize.Height + 1)));
                        screen.Location = new Point(
                            screenbg.Width / 2 - screen.Size.Width / 2,
                            screenbg.Height / 2 - screen.Size.Height / 2
                        );

                        WindowSize = new Size(windowSize.Width + 1, windowSize.Height);
                    }
                    else
                    {
                        normalSize = true;
                    }
                }
                else
                {
                    normalSize = true;
                }
            }
            else
            {
                normalSize = true;
            }

            if (normalSize)
            {
                screen.Location = new Point(0, 0);
                screen.Size = new Size(720, 456);
                WindowSize = new Size(80, 24);
            }
        }
        #endregion
    }
}
