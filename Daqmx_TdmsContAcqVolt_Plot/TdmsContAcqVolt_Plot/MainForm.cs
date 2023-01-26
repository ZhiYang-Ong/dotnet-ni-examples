/******************************************************************************
*
* Example program:
*   TdmsContAcqVolt_Plot
*
* Category:
*   AI
*
* Description:
*   This example demonstrates how to acquire a continuous amount while
*   simultaneously streaming that data to a binary file.
*
* Instructions for running:
*   1.  Select the physical channel corresponding to where your signal is input
*       on the DAQ device.
*   2.  Enter the minimum and maximum voltage values.Note: For better accuracy,
*       try to match the input range to the expected voltage level of the
*       measured signal.
*   3.  Set the rate of the acquisition.Note: The rate should be at least twice
*       as fast as the maximum frequency component of the signal being acquired.
*       Also, in order to avoid Error -50410 (buffer overflow) it is important
*       to make sure the rate and the number of samples to read per iteration
*       are set such that they don't fill the buffer too quickly. If this error
*       occurs, try reducing the rate or increasing the number of samples to
*       read per iteration.
*   4.  Set the file to write to.
*
* Steps:
*   1.  Create a new analog input task.
*   2.  Create an analog input voltage channel.
*   3.  Set up the timing for the acquisition. In this example, we use the DAQ
*       device's internal clock to continuously acquire samples.
*   4.  Configure the task to enable TDMS logging.
*   5.  Call AnalogMultiChannelReader.BeginReadWaveform to install a callback
*       and begin the asynchronous read operation.
*   6.  Inside the callback, call AnalogMultiChannelReader.EndReadWaveforme to
*       retrieve the data from the read operation.  
*   7.  Call AnalogMultiChannelReader.BeginMemoryOptimizedReadWaveform
*   8.  Dispose the Task object to clean-up any resources associated with the
*       task.
*   9.  Handle any DaqExceptions, if they occur.
*
*   Note: This example sets SynchronizeCallback to true. If SynchronizeCallback
*   is set to false, then you must give special consideration to safely dispose
*   the task and to update the UI from the callback. If SynchronizeCallback is
*   set to false, the callback executes on the worker thread and not on the main
*   UI thread. You can only update a UI component on the thread on which it was
*   created. Refer to the How to: Safely Dispose Task When Using Asynchronous
*   Callbacks topic in the NI-DAQmx .NET help for more information.
*
* I/O Connections Overview:
*   Make sure your signal input terminals match the physical channel text box. 
*   For more information on the input and output terminals for your device, open
*   the NI-DAQmx Help, and refer to the NI-DAQmx Device Terminals and Device
*   Considerations books in the table of contents.
*
* Microsoft Windows Vista User Account Control
*   Running certain applications on Microsoft Windows Vista requires
*   administrator privileges, 
*   because the application name contains keywords such as setup, update, or
*   install. To avoid this problem, 
*   you must add an additional manifest to the application that specifies the
*   privileges required to run 
*   the application. Some Measurement Studio NI-DAQmx examples for Visual Studio
*   include these keywords. 
*   Therefore, all examples for Visual Studio are shipped with an additional
*   manifest file that you must 
*   embed in the example executable. The manifest file is named
*   [ExampleName].exe.manifest, where [ExampleName] 
*   is the NI-provided example name. For information on how to embed the manifest
*   file, refer to http://msdn2.microsoft.com/en-us/library/bb756929.aspx.Note: 
*   The manifest file is not provided with examples for Visual Studio .NET 2003.
*
******************************************************************************/

using System;
using System.Windows.Forms;
using System.Data;
using NationalInstruments.DAQmx;
using ScottPlot;
using NationalInstruments.Restricted;
using System.Collections.Generic;
using PhyChanPopup;

namespace NationalInstruments.Examples.TdmsContAcqVolt_Plot
{
    /// <summary>
    /// Summary description for MainForm.
    /// </summary>
    public class MainForm : System.Windows.Forms.Form
    {
        private GroupBox channelParametersGroupBox;
        private Label physicalChannelLabel;
        private Button browseChanButton;
        private Label minimumLabel;
        private Label maximumLabel;
        private Label termCfgLabel;
        private ComboBox physicalChannelComboBox;
        internal NumericUpDown minimumValueNumeric;
        internal NumericUpDown maximumValueNumeric;
        private ComboBox termCfgComboBox;

        private GroupBox timingParametersGroupBox;
        private Label rateLabel;
        private Label samplesLabel;
        private Label clkSrcLabel;
        private Label actualRateLabel;
        private NumericUpDown rateNumeric;
        private NumericUpDown samplesPerChannelNumeric;
        private ComboBox clkSrcComboBox;
        private TextBox actualRateTextBox;

        private GroupBox loggingParametersGroupBox;
        private Label TdmsFilePathLabel;
        private TextBox tdmsFilePathTextBox;
        private OpenFileDialog openFileDialog1;

        private GroupBox acquisitionResultGroupBox;
        private Label resultLabel;
        private ScottPlot.FormsPlot acqPlot;

        private Button startButton;
        private Button stopButton;
        private Button browseFileButton;
        
        private AnalogMultiChannelReader analogInReader;
        private Task myTask;
        private Task runningTask;
        private AsyncCallback analogCallback;
        private AnalogWaveform<double>[] data;

        private GroupBox trigParametersGroupBox;
        private TabControl trigTabControl;
        private TabPage noTrigTab;  // No Trigger
        private Label noTrigLabel;
        private TabPage digStartTab;    // Digital Start Trigger
        private Label digStartSrcLabel;
        private Label digStartEdgeLabel;
        private ComboBox digStartSrcComboBox;
        private ComboBox digStartEdgeComboBox;
        private TabPage digPauseTab;    // Digital Pause Trigger
        private Label digPauseWhenLabel;
        private ComboBox digPauseWhenComboBox;
        private Label digPauseSrcLabel;
        private ComboBox digPauseSrcComboBox;
        private TabPage digRefTab;  // Digital Reference Trigger
        private Label digRefLabel;
        private TabPage anlgStartTab;   // Analog Start Trigger
        private Label anlgStartSrcLabel;
        private Label anlgStartSlopeLabel;
        private Label anlgStartLevelLabel;
        private Label anlgStartHysteresisLabel;
        private ComboBox anlgStartSrcComboBox;
        private ComboBox anlgStartSlopeComboBox;
        internal NumericUpDown anlgStartLevelNumeric;
        internal NumericUpDown anlgStartHysteresisNumeric;
        private TabPage anlgPauseTab;   // Analog Pause Trigger
        private Label anlgPauseSrcLabel;
        private Label anlgPauseWhenLabel;
        private Label anlgPauseLevelLabel;
        private ComboBox anlgPauseSrcComboBox;
        private ComboBox anlgPauseWhenComboBox;
        internal NumericUpDown anlgPauseLevelNumeric;
        private TabPage anlgRefTab; // Analog Reference Trigger
        private Label anlgRefLabel;
        private TabPage timeStartTab;   // Time Start Trigger
        private Label timeStartNote;
        private CheckBox manualTimeCheckBox;
        private DateTimePicker timeStartPicker;
        private ContextMenuStrip contextMenuStrip1;
        private System.ComponentModel.IContainer components;

        public MainForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
            stopButton.Enabled = false;

            physicalChannelComboBox.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External));
            if (physicalChannelComboBox.Items.Count > 0)
                physicalChannelComboBox.SelectedIndex = 0;

            termCfgComboBox.DataSource = Enum.GetValues(typeof(termConfig));
            termCfgComboBox.SelectedIndex = 4;

            clkSrcComboBox.Items.AddRange(DaqSystem.Local.GetTerminals(TerminalTypes.All));

            string[] basicTerminals = DaqSystem.Local.GetTerminals(TerminalTypes.Basic);
            digStartSrcComboBox.Items.AddRange(basicTerminals);
            digStartEdgeComboBox.DataSource = Enum.GetValues(typeof(DigitalEdgeStartTriggerEdge));
            digPauseSrcComboBox.Items.AddRange(basicTerminals);
            digPauseWhenComboBox.DataSource = Enum.GetValues(typeof(DigitalLevelPauseTriggerCondition));
            
            // Keep only APFI for analog trigger.
            List<string> apfiTerms = new List<string>();
            foreach (string terminal in basicTerminals)
            {
                if (terminal.Contains("APFI"))
                    apfiTerms.Add(terminal);
            }
            string[] afiTerminals = apfiTerms.ToArray();
            anlgStartSrcComboBox.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External));
            anlgStartSrcComboBox.Items.AddRange(afiTerminals);
            if (anlgStartSrcComboBox.Items.Count > 0)
                anlgStartSrcComboBox.SelectedIndex = 0;
            anlgPauseSrcComboBox.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External));
            anlgPauseSrcComboBox.Items.AddRange(afiTerminals);
            if (anlgPauseSrcComboBox.Items.Count > 0)
                anlgPauseSrcComboBox.SelectedIndex = 0;
            anlgStartSlopeComboBox.DataSource = Enum.GetValues(typeof(AnalogEdgeStartTriggerSlope));
            anlgPauseWhenComboBox.DataSource = Enum.GetValues(typeof(AnalogLevelPauseTriggerCondition));
            timeStartPicker.Value = DateTime.Now;
        }

        public enum termConfig
        {
            Default = -1,
            RSE = 10083,
            NRSE = 10078,
            Diffential = 10106,
            Pseudodifferential = 12529
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
                if (myTask != null)
                {
                    runningTask = null;
                    myTask.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.channelParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.browseChanButton = new System.Windows.Forms.Button();
            this.termCfgComboBox = new System.Windows.Forms.ComboBox();
            this.physicalChannelComboBox = new System.Windows.Forms.ComboBox();
            this.minimumValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.maximumValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.maximumLabel = new System.Windows.Forms.Label();
            this.minimumLabel = new System.Windows.Forms.Label();
            this.termCfgLabel = new System.Windows.Forms.Label();
            this.physicalChannelLabel = new System.Windows.Forms.Label();
            this.timingParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.actualRateTextBox = new System.Windows.Forms.TextBox();
            this.rateNumeric = new System.Windows.Forms.NumericUpDown();
            this.clkSrcComboBox = new System.Windows.Forms.ComboBox();
            this.samplesLabel = new System.Windows.Forms.Label();
            this.clkSrcLabel = new System.Windows.Forms.Label();
            this.actualRateLabel = new System.Windows.Forms.Label();
            this.rateLabel = new System.Windows.Forms.Label();
            this.samplesPerChannelNumeric = new System.Windows.Forms.NumericUpDown();
            this.startButton = new System.Windows.Forms.Button();
            this.stopButton = new System.Windows.Forms.Button();
            this.acquisitionResultGroupBox = new System.Windows.Forms.GroupBox();
            this.acqPlot = new ScottPlot.FormsPlot();
            this.resultLabel = new System.Windows.Forms.Label();
            this.loggingParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.tdmsFilePathTextBox = new System.Windows.Forms.TextBox();
            this.TdmsFilePathLabel = new System.Windows.Forms.Label();
            this.browseFileButton = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.trigTabControl = new System.Windows.Forms.TabControl();
            this.noTrigTab = new System.Windows.Forms.TabPage();
            this.noTrigLabel = new System.Windows.Forms.Label();
            this.digStartTab = new System.Windows.Forms.TabPage();
            this.digStartEdgeComboBox = new System.Windows.Forms.ComboBox();
            this.digStartEdgeLabel = new System.Windows.Forms.Label();
            this.digStartSrcComboBox = new System.Windows.Forms.ComboBox();
            this.digStartSrcLabel = new System.Windows.Forms.Label();
            this.digPauseTab = new System.Windows.Forms.TabPage();
            this.digPauseWhenComboBox = new System.Windows.Forms.ComboBox();
            this.digPauseWhenLabel = new System.Windows.Forms.Label();
            this.digPauseSrcComboBox = new System.Windows.Forms.ComboBox();
            this.digPauseSrcLabel = new System.Windows.Forms.Label();
            this.digRefTab = new System.Windows.Forms.TabPage();
            this.digRefLabel = new System.Windows.Forms.Label();
            this.anlgStartTab = new System.Windows.Forms.TabPage();
            this.anlgStartSrcComboBox = new System.Windows.Forms.ComboBox();
            this.anlgStartHysteresisLabel = new System.Windows.Forms.Label();
            this.anlgStartLevelLabel = new System.Windows.Forms.Label();
            this.anlgStartLevelNumeric = new System.Windows.Forms.NumericUpDown();
            this.anlgStartHysteresisNumeric = new System.Windows.Forms.NumericUpDown();
            this.anlgStartSlopeComboBox = new System.Windows.Forms.ComboBox();
            this.anlgStartSlopeLabel = new System.Windows.Forms.Label();
            this.anlgStartSrcLabel = new System.Windows.Forms.Label();
            this.anlgPauseTab = new System.Windows.Forms.TabPage();
            this.anlgPauseSrcComboBox = new System.Windows.Forms.ComboBox();
            this.anlgPauseLevelLabel = new System.Windows.Forms.Label();
            this.anlgPauseLevelNumeric = new System.Windows.Forms.NumericUpDown();
            this.anlgPauseWhenComboBox = new System.Windows.Forms.ComboBox();
            this.anlgPauseWhenLabel = new System.Windows.Forms.Label();
            this.anlgPauseSrcLabel = new System.Windows.Forms.Label();
            this.anlgRefTab = new System.Windows.Forms.TabPage();
            this.anlgRefLabel = new System.Windows.Forms.Label();
            this.timeStartTab = new System.Windows.Forms.TabPage();
            this.timeStartNote = new System.Windows.Forms.Label();
            this.manualTimeCheckBox = new System.Windows.Forms.CheckBox();
            this.timeStartPicker = new System.Windows.Forms.DateTimePicker();
            this.trigParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.channelParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.minimumValueNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.maximumValueNumeric)).BeginInit();
            this.timingParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.rateNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.samplesPerChannelNumeric)).BeginInit();
            this.acquisitionResultGroupBox.SuspendLayout();
            this.loggingParametersGroupBox.SuspendLayout();
            this.trigTabControl.SuspendLayout();
            this.noTrigTab.SuspendLayout();
            this.digStartTab.SuspendLayout();
            this.digPauseTab.SuspendLayout();
            this.digRefTab.SuspendLayout();
            this.anlgStartTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.anlgStartLevelNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.anlgStartHysteresisNumeric)).BeginInit();
            this.anlgPauseTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.anlgPauseLevelNumeric)).BeginInit();
            this.anlgRefTab.SuspendLayout();
            this.timeStartTab.SuspendLayout();
            this.trigParametersGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // channelParametersGroupBox
            // 
            this.channelParametersGroupBox.Controls.Add(this.browseChanButton);
            this.channelParametersGroupBox.Controls.Add(this.termCfgComboBox);
            this.channelParametersGroupBox.Controls.Add(this.physicalChannelComboBox);
            this.channelParametersGroupBox.Controls.Add(this.minimumValueNumeric);
            this.channelParametersGroupBox.Controls.Add(this.maximumValueNumeric);
            this.channelParametersGroupBox.Controls.Add(this.maximumLabel);
            this.channelParametersGroupBox.Controls.Add(this.minimumLabel);
            this.channelParametersGroupBox.Controls.Add(this.termCfgLabel);
            this.channelParametersGroupBox.Controls.Add(this.physicalChannelLabel);
            this.channelParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.channelParametersGroupBox.Location = new System.Drawing.Point(13, 12);
            this.channelParametersGroupBox.Name = "channelParametersGroupBox";
            this.channelParametersGroupBox.Size = new System.Drawing.Size(329, 213);
            this.channelParametersGroupBox.TabIndex = 2;
            this.channelParametersGroupBox.TabStop = false;
            this.channelParametersGroupBox.Text = "Channel Parameters";
            // 
            // browseChanButton
            // 
            this.browseChanButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.browseChanButton.Location = new System.Drawing.Point(262, 55);
            this.browseChanButton.Name = "browseChanButton";
            this.browseChanButton.Size = new System.Drawing.Size(61, 26);
            this.browseChanButton.TabIndex = 6;
            this.browseChanButton.Text = "Browse";
            this.browseChanButton.Click += new System.EventHandler(this.browseChanButton_Click);
            // 
            // termCfgComboBox
            // 
            this.termCfgComboBox.Location = new System.Drawing.Point(17, 174);
            this.termCfgComboBox.Name = "termCfgComboBox";
            this.termCfgComboBox.Size = new System.Drawing.Size(287, 28);
            this.termCfgComboBox.TabIndex = 1;
            this.termCfgComboBox.Text = "Default";
            // 
            // physicalChannelComboBox
            // 
            this.physicalChannelComboBox.Location = new System.Drawing.Point(18, 55);
            this.physicalChannelComboBox.Name = "physicalChannelComboBox";
            this.physicalChannelComboBox.Size = new System.Drawing.Size(240, 28);
            this.physicalChannelComboBox.TabIndex = 1;
            this.physicalChannelComboBox.Text = "Dev1/ai0";
            // 
            // minimumValueNumeric
            // 
            this.minimumValueNumeric.DecimalPlaces = 1;
            this.minimumValueNumeric.Location = new System.Drawing.Point(18, 110);
            this.minimumValueNumeric.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.minimumValueNumeric.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            -2147483648});
            this.minimumValueNumeric.Name = "minimumValueNumeric";
            this.minimumValueNumeric.Size = new System.Drawing.Size(137, 26);
            this.minimumValueNumeric.TabIndex = 3;
            this.minimumValueNumeric.Value = new decimal(new int[] {
            100,
            0,
            0,
            -2147418112});
            // 
            // maximumValueNumeric
            // 
            this.maximumValueNumeric.DecimalPlaces = 1;
            this.maximumValueNumeric.Location = new System.Drawing.Point(167, 110);
            this.maximumValueNumeric.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.maximumValueNumeric.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            -2147483648});
            this.maximumValueNumeric.Name = "maximumValueNumeric";
            this.maximumValueNumeric.Size = new System.Drawing.Size(137, 26);
            this.maximumValueNumeric.TabIndex = 5;
            this.maximumValueNumeric.Value = new decimal(new int[] {
            100,
            0,
            0,
            65536});
            // 
            // maximumLabel
            // 
            this.maximumLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.maximumLabel.Location = new System.Drawing.Point(168, 87);
            this.maximumLabel.Name = "maximumLabel";
            this.maximumLabel.Size = new System.Drawing.Size(115, 25);
            this.maximumLabel.TabIndex = 4;
            this.maximumLabel.Text = "Max Value (V):";
            // 
            // minimumLabel
            // 
            this.minimumLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.minimumLabel.Location = new System.Drawing.Point(20, 89);
            this.minimumLabel.Name = "minimumLabel";
            this.minimumLabel.Size = new System.Drawing.Size(106, 23);
            this.minimumLabel.TabIndex = 2;
            this.minimumLabel.Text = "Min Value (V):";
            // 
            // termCfgLabel
            // 
            this.termCfgLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.termCfgLabel.Location = new System.Drawing.Point(18, 150);
            this.termCfgLabel.Name = "termCfgLabel";
            this.termCfgLabel.Size = new System.Drawing.Size(166, 24);
            this.termCfgLabel.TabIndex = 0;
            this.termCfgLabel.Text = "Terminal Configuration:";
            // 
            // physicalChannelLabel
            // 
            this.physicalChannelLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.physicalChannelLabel.Location = new System.Drawing.Point(18, 32);
            this.physicalChannelLabel.Name = "physicalChannelLabel";
            this.physicalChannelLabel.Size = new System.Drawing.Size(153, 23);
            this.physicalChannelLabel.TabIndex = 0;
            this.physicalChannelLabel.Text = "Physical Channel:";
            // 
            // timingParametersGroupBox
            // 
            this.timingParametersGroupBox.Controls.Add(this.actualRateTextBox);
            this.timingParametersGroupBox.Controls.Add(this.rateNumeric);
            this.timingParametersGroupBox.Controls.Add(this.clkSrcComboBox);
            this.timingParametersGroupBox.Controls.Add(this.samplesLabel);
            this.timingParametersGroupBox.Controls.Add(this.clkSrcLabel);
            this.timingParametersGroupBox.Controls.Add(this.actualRateLabel);
            this.timingParametersGroupBox.Controls.Add(this.rateLabel);
            this.timingParametersGroupBox.Controls.Add(this.samplesPerChannelNumeric);
            this.timingParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.timingParametersGroupBox.Location = new System.Drawing.Point(13, 231);
            this.timingParametersGroupBox.Name = "timingParametersGroupBox";
            this.timingParametersGroupBox.Size = new System.Drawing.Size(329, 226);
            this.timingParametersGroupBox.TabIndex = 3;
            this.timingParametersGroupBox.TabStop = false;
            this.timingParametersGroupBox.Text = "Timing Parameters";
            // 
            // actualRateTextBox
            // 
            this.actualRateTextBox.Location = new System.Drawing.Point(172, 116);
            this.actualRateTextBox.Name = "actualRateTextBox";
            this.actualRateTextBox.ReadOnly = true;
            this.actualRateTextBox.Size = new System.Drawing.Size(132, 26);
            this.actualRateTextBox.TabIndex = 4;
            // 
            // rateNumeric
            // 
            this.rateNumeric.Location = new System.Drawing.Point(18, 116);
            this.rateNumeric.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.rateNumeric.Name = "rateNumeric";
            this.rateNumeric.Size = new System.Drawing.Size(137, 26);
            this.rateNumeric.TabIndex = 3;
            this.rateNumeric.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // clkSrcComboBox
            // 
            this.clkSrcComboBox.Location = new System.Drawing.Point(18, 53);
            this.clkSrcComboBox.Name = "clkSrcComboBox";
            this.clkSrcComboBox.Size = new System.Drawing.Size(286, 28);
            this.clkSrcComboBox.TabIndex = 1;
            this.clkSrcComboBox.Text = "OnboardClock";
            // 
            // samplesLabel
            // 
            this.samplesLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.samplesLabel.Location = new System.Drawing.Point(20, 148);
            this.samplesLabel.Name = "samplesLabel";
            this.samplesLabel.Size = new System.Drawing.Size(166, 23);
            this.samplesLabel.TabIndex = 0;
            this.samplesLabel.Text = "Samples/Channel:";
            // 
            // clkSrcLabel
            // 
            this.clkSrcLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.clkSrcLabel.Location = new System.Drawing.Point(20, 33);
            this.clkSrcLabel.Name = "clkSrcLabel";
            this.clkSrcLabel.Size = new System.Drawing.Size(166, 26);
            this.clkSrcLabel.TabIndex = 2;
            this.clkSrcLabel.Text = "Sample Clock Source:";
            // 
            // actualRateLabel
            // 
            this.actualRateLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.actualRateLabel.Location = new System.Drawing.Point(171, 93);
            this.actualRateLabel.Name = "actualRateLabel";
            this.actualRateLabel.Size = new System.Drawing.Size(142, 26);
            this.actualRateLabel.TabIndex = 2;
            this.actualRateLabel.Text = "Actual Rate (Hz):";
            // 
            // rateLabel
            // 
            this.rateLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.rateLabel.Location = new System.Drawing.Point(20, 93);
            this.rateLabel.Name = "rateLabel";
            this.rateLabel.Size = new System.Drawing.Size(142, 26);
            this.rateLabel.TabIndex = 2;
            this.rateLabel.Text = "Sample Rate (Hz):";
            // 
            // samplesPerChannelNumeric
            // 
            this.samplesPerChannelNumeric.Location = new System.Drawing.Point(19, 171);
            this.samplesPerChannelNumeric.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.samplesPerChannelNumeric.Name = "samplesPerChannelNumeric";
            this.samplesPerChannelNumeric.Size = new System.Drawing.Size(137, 26);
            this.samplesPerChannelNumeric.TabIndex = 1;
            this.samplesPerChannelNumeric.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // startButton
            // 
            this.startButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.startButton.Location = new System.Drawing.Point(30, 574);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(128, 35);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // stopButton
            // 
            this.stopButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.stopButton.Location = new System.Drawing.Point(180, 574);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(128, 35);
            this.stopButton.TabIndex = 1;
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
            // 
            // acquisitionResultGroupBox
            // 
            this.acquisitionResultGroupBox.Controls.Add(this.acqPlot);
            this.acquisitionResultGroupBox.Controls.Add(this.resultLabel);
            this.acquisitionResultGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.acquisitionResultGroupBox.Location = new System.Drawing.Point(353, 12);
            this.acquisitionResultGroupBox.Name = "acquisitionResultGroupBox";
            this.acquisitionResultGroupBox.Size = new System.Drawing.Size(562, 390);
            this.acquisitionResultGroupBox.TabIndex = 4;
            this.acquisitionResultGroupBox.TabStop = false;
            this.acquisitionResultGroupBox.Text = "Acquisition Results";
            // 
            // acqPlot
            // 
            this.acqPlot.Location = new System.Drawing.Point(5, 46);
            this.acqPlot.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.acqPlot.Name = "acqPlot";
            this.acqPlot.Size = new System.Drawing.Size(549, 332);
            this.acqPlot.TabIndex = 2;
            // 
            // resultLabel
            // 
            this.resultLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.resultLabel.Location = new System.Drawing.Point(13, 23);
            this.resultLabel.Name = "resultLabel";
            this.resultLabel.Size = new System.Drawing.Size(179, 24);
            this.resultLabel.TabIndex = 0;
            this.resultLabel.Text = "Acquisition Data (V):";
            // 
            // loggingParametersGroupBox
            // 
            this.loggingParametersGroupBox.Controls.Add(this.tdmsFilePathTextBox);
            this.loggingParametersGroupBox.Controls.Add(this.TdmsFilePathLabel);
            this.loggingParametersGroupBox.Controls.Add(this.browseFileButton);
            this.loggingParametersGroupBox.Location = new System.Drawing.Point(12, 463);
            this.loggingParametersGroupBox.Name = "loggingParametersGroupBox";
            this.loggingParametersGroupBox.Size = new System.Drawing.Size(330, 105);
            this.loggingParametersGroupBox.TabIndex = 5;
            this.loggingParametersGroupBox.TabStop = false;
            this.loggingParametersGroupBox.Text = "Logging Parameters";
            // 
            // tdmsFilePathTextBox
            // 
            this.tdmsFilePathTextBox.Location = new System.Drawing.Point(19, 56);
            this.tdmsFilePathTextBox.Name = "tdmsFilePathTextBox";
            this.tdmsFilePathTextBox.Size = new System.Drawing.Size(240, 26);
            this.tdmsFilePathTextBox.TabIndex = 1;
            // 
            // TdmsFilePathLabel
            // 
            this.TdmsFilePathLabel.AutoSize = true;
            this.TdmsFilePathLabel.Location = new System.Drawing.Point(19, 33);
            this.TdmsFilePathLabel.Name = "TdmsFilePathLabel";
            this.TdmsFilePathLabel.Size = new System.Drawing.Size(295, 20);
            this.TdmsFilePathLabel.TabIndex = 0;
            this.TdmsFilePathLabel.Text = "TDMS File Path (Leave blank to disable):";
            // 
            // browseFileButton
            // 
            this.browseFileButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.browseFileButton.Location = new System.Drawing.Point(263, 56);
            this.browseFileButton.Name = "browseFileButton";
            this.browseFileButton.Size = new System.Drawing.Size(61, 26);
            this.browseFileButton.TabIndex = 0;
            this.browseFileButton.Text = "Browse";
            this.browseFileButton.Click += new System.EventHandler(this.browseButton_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // trigTabControl
            // 
            this.trigTabControl.Controls.Add(this.noTrigTab);
            this.trigTabControl.Controls.Add(this.digStartTab);
            this.trigTabControl.Controls.Add(this.digPauseTab);
            this.trigTabControl.Controls.Add(this.digRefTab);
            this.trigTabControl.Controls.Add(this.anlgStartTab);
            this.trigTabControl.Controls.Add(this.anlgPauseTab);
            this.trigTabControl.Controls.Add(this.anlgRefTab);
            this.trigTabControl.Controls.Add(this.timeStartTab);
            this.trigTabControl.Location = new System.Drawing.Point(17, 22);
            this.trigTabControl.Multiline = true;
            this.trigTabControl.Name = "trigTabControl";
            this.trigTabControl.SelectedIndex = 0;
            this.trigTabControl.Size = new System.Drawing.Size(530, 177);
            this.trigTabControl.SizeMode = System.Windows.Forms.TabSizeMode.FillToRight;
            this.trigTabControl.TabIndex = 8;
            // 
            // noTrigTab
            // 
            this.noTrigTab.Controls.Add(this.noTrigLabel);
            this.noTrigTab.Location = new System.Drawing.Point(4, 54);
            this.noTrigTab.Name = "noTrigTab";
            this.noTrigTab.Padding = new System.Windows.Forms.Padding(3);
            this.noTrigTab.Size = new System.Drawing.Size(522, 119);
            this.noTrigTab.TabIndex = 0;
            this.noTrigTab.Text = "No Trigger";
            this.noTrigTab.UseVisualStyleBackColor = true;
            // 
            // noTrigLabel
            // 
            this.noTrigLabel.AutoSize = true;
            this.noTrigLabel.Location = new System.Drawing.Point(28, 8);
            this.noTrigLabel.Name = "noTrigLabel";
            this.noTrigLabel.Size = new System.Drawing.Size(469, 80);
            this.noTrigLabel.TabIndex = 0;
            this.noTrigLabel.Text = "To enable triggers, select a tab above, and configure the settings.\r\n\r\nNot all ha" +
    "rdware supports all trigger types. Refer to your device \r\ndocumentation for more" +
    " information.";
            // 
            // digStartTab
            // 
            this.digStartTab.Controls.Add(this.digStartEdgeComboBox);
            this.digStartTab.Controls.Add(this.digStartEdgeLabel);
            this.digStartTab.Controls.Add(this.digStartSrcComboBox);
            this.digStartTab.Controls.Add(this.digStartSrcLabel);
            this.digStartTab.Location = new System.Drawing.Point(4, 54);
            this.digStartTab.Name = "digStartTab";
            this.digStartTab.Padding = new System.Windows.Forms.Padding(3);
            this.digStartTab.Size = new System.Drawing.Size(522, 119);
            this.digStartTab.TabIndex = 1;
            this.digStartTab.Text = "Digital Start";
            this.digStartTab.UseVisualStyleBackColor = true;
            // 
            // digStartEdgeComboBox
            // 
            this.digStartEdgeComboBox.FormattingEnabled = true;
            this.digStartEdgeComboBox.Location = new System.Drawing.Point(379, 41);
            this.digStartEdgeComboBox.Name = "digStartEdgeComboBox";
            this.digStartEdgeComboBox.Size = new System.Drawing.Size(116, 28);
            this.digStartEdgeComboBox.TabIndex = 1;
            // 
            // digStartEdgeLabel
            // 
            this.digStartEdgeLabel.AutoSize = true;
            this.digStartEdgeLabel.Location = new System.Drawing.Point(377, 18);
            this.digStartEdgeLabel.Name = "digStartEdgeLabel";
            this.digStartEdgeLabel.Size = new System.Drawing.Size(51, 20);
            this.digStartEdgeLabel.TabIndex = 0;
            this.digStartEdgeLabel.Text = "Edge:";
            this.digStartEdgeLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // digStartSrcComboBox
            // 
            this.digStartSrcComboBox.FormattingEnabled = true;
            this.digStartSrcComboBox.Location = new System.Drawing.Point(32, 41);
            this.digStartSrcComboBox.Name = "digStartSrcComboBox";
            this.digStartSrcComboBox.Size = new System.Drawing.Size(319, 28);
            this.digStartSrcComboBox.TabIndex = 1;
            // 
            // digStartSrcLabel
            // 
            this.digStartSrcLabel.AutoSize = true;
            this.digStartSrcLabel.Location = new System.Drawing.Point(28, 18);
            this.digStartSrcLabel.Name = "digStartSrcLabel";
            this.digStartSrcLabel.Size = new System.Drawing.Size(64, 20);
            this.digStartSrcLabel.TabIndex = 0;
            this.digStartSrcLabel.Text = "Source:";
            // 
            // digPauseTab
            // 
            this.digPauseTab.Controls.Add(this.digPauseWhenComboBox);
            this.digPauseTab.Controls.Add(this.digPauseWhenLabel);
            this.digPauseTab.Controls.Add(this.digPauseSrcComboBox);
            this.digPauseTab.Controls.Add(this.digPauseSrcLabel);
            this.digPauseTab.Location = new System.Drawing.Point(4, 54);
            this.digPauseTab.Name = "digPauseTab";
            this.digPauseTab.Padding = new System.Windows.Forms.Padding(3);
            this.digPauseTab.Size = new System.Drawing.Size(522, 119);
            this.digPauseTab.TabIndex = 2;
            this.digPauseTab.Text = "Digital Pause";
            this.digPauseTab.UseVisualStyleBackColor = true;
            // 
            // digPauseWhenComboBox
            // 
            this.digPauseWhenComboBox.FormattingEnabled = true;
            this.digPauseWhenComboBox.Location = new System.Drawing.Point(379, 41);
            this.digPauseWhenComboBox.Name = "digPauseWhenComboBox";
            this.digPauseWhenComboBox.Size = new System.Drawing.Size(116, 28);
            this.digPauseWhenComboBox.TabIndex = 4;
            // 
            // digPauseWhenLabel
            // 
            this.digPauseWhenLabel.AutoSize = true;
            this.digPauseWhenLabel.Location = new System.Drawing.Point(377, 18);
            this.digPauseWhenLabel.Name = "digPauseWhenLabel";
            this.digPauseWhenLabel.Size = new System.Drawing.Size(104, 20);
            this.digPauseWhenLabel.TabIndex = 2;
            this.digPauseWhenLabel.Text = "Pause When:";
            this.digPauseWhenLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // digPauseSrcComboBox
            // 
            this.digPauseSrcComboBox.FormattingEnabled = true;
            this.digPauseSrcComboBox.Location = new System.Drawing.Point(32, 41);
            this.digPauseSrcComboBox.Name = "digPauseSrcComboBox";
            this.digPauseSrcComboBox.Size = new System.Drawing.Size(319, 28);
            this.digPauseSrcComboBox.TabIndex = 5;
            // 
            // digPauseSrcLabel
            // 
            this.digPauseSrcLabel.AutoSize = true;
            this.digPauseSrcLabel.Location = new System.Drawing.Point(28, 18);
            this.digPauseSrcLabel.Name = "digPauseSrcLabel";
            this.digPauseSrcLabel.Size = new System.Drawing.Size(64, 20);
            this.digPauseSrcLabel.TabIndex = 3;
            this.digPauseSrcLabel.Text = "Source:";
            // 
            // digRefTab
            // 
            this.digRefTab.Controls.Add(this.digRefLabel);
            this.digRefTab.Location = new System.Drawing.Point(4, 54);
            this.digRefTab.Name = "digRefTab";
            this.digRefTab.Padding = new System.Windows.Forms.Padding(3);
            this.digRefTab.Size = new System.Drawing.Size(522, 119);
            this.digRefTab.TabIndex = 3;
            this.digRefTab.Text = "Digital Reference";
            this.digRefTab.UseVisualStyleBackColor = true;
            // 
            // digRefLabel
            // 
            this.digRefLabel.AutoSize = true;
            this.digRefLabel.Location = new System.Drawing.Point(37, 11);
            this.digRefLabel.Name = "digRefLabel";
            this.digRefLabel.Size = new System.Drawing.Size(446, 80);
            this.digRefLabel.TabIndex = 1;
            this.digRefLabel.Text = "This trigger type is not supported in continuous sample timing. \r\n\r\nRefer to your" +
    " device documentation for more information \r\non which triggers are supported.";
            // 
            // anlgStartTab
            // 
            this.anlgStartTab.Controls.Add(this.anlgStartSrcComboBox);
            this.anlgStartTab.Controls.Add(this.anlgStartHysteresisLabel);
            this.anlgStartTab.Controls.Add(this.anlgStartLevelLabel);
            this.anlgStartTab.Controls.Add(this.anlgStartLevelNumeric);
            this.anlgStartTab.Controls.Add(this.anlgStartHysteresisNumeric);
            this.anlgStartTab.Controls.Add(this.anlgStartSlopeComboBox);
            this.anlgStartTab.Controls.Add(this.anlgStartSlopeLabel);
            this.anlgStartTab.Controls.Add(this.anlgStartSrcLabel);
            this.anlgStartTab.Location = new System.Drawing.Point(4, 54);
            this.anlgStartTab.Name = "anlgStartTab";
            this.anlgStartTab.Padding = new System.Windows.Forms.Padding(3);
            this.anlgStartTab.Size = new System.Drawing.Size(522, 119);
            this.anlgStartTab.TabIndex = 4;
            this.anlgStartTab.Text = "Analog Start";
            this.anlgStartTab.UseVisualStyleBackColor = true;
            // 
            // anlgStartSrcComboBox
            // 
            this.anlgStartSrcComboBox.Location = new System.Drawing.Point(30, 31);
            this.anlgStartSrcComboBox.Name = "anlgStartSrcComboBox";
            this.anlgStartSrcComboBox.Size = new System.Drawing.Size(286, 28);
            this.anlgStartSrcComboBox.TabIndex = 12;
            this.anlgStartSrcComboBox.Text = "APFI0";
            // 
            // anlgStartHysteresisLabel
            // 
            this.anlgStartHysteresisLabel.AutoSize = true;
            this.anlgStartHysteresisLabel.Location = new System.Drawing.Point(263, 74);
            this.anlgStartHysteresisLabel.Name = "anlgStartHysteresisLabel";
            this.anlgStartHysteresisLabel.Size = new System.Drawing.Size(87, 20);
            this.anlgStartHysteresisLabel.TabIndex = 10;
            this.anlgStartHysteresisLabel.Text = "Hysteresis:";
            this.anlgStartHysteresisLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // anlgStartLevelLabel
            // 
            this.anlgStartLevelLabel.AutoSize = true;
            this.anlgStartLevelLabel.Location = new System.Drawing.Point(26, 74);
            this.anlgStartLevelLabel.Name = "anlgStartLevelLabel";
            this.anlgStartLevelLabel.Size = new System.Drawing.Size(50, 20);
            this.anlgStartLevelLabel.TabIndex = 11;
            this.anlgStartLevelLabel.Text = "Level:";
            // 
            // anlgStartLevelNumeric
            // 
            this.anlgStartLevelNumeric.DecimalPlaces = 2;
            this.anlgStartLevelNumeric.Location = new System.Drawing.Point(82, 72);
            this.anlgStartLevelNumeric.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.anlgStartLevelNumeric.Minimum = new decimal(new int[] {
            1000000,
            0,
            0,
            -2147483648});
            this.anlgStartLevelNumeric.Name = "anlgStartLevelNumeric";
            this.anlgStartLevelNumeric.Size = new System.Drawing.Size(137, 26);
            this.anlgStartLevelNumeric.TabIndex = 7;
            // 
            // anlgStartHysteresisNumeric
            // 
            this.anlgStartHysteresisNumeric.DecimalPlaces = 1;
            this.anlgStartHysteresisNumeric.Location = new System.Drawing.Point(356, 72);
            this.anlgStartHysteresisNumeric.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.anlgStartHysteresisNumeric.Minimum = new decimal(new int[] {
            100000,
            0,
            0,
            -2147483648});
            this.anlgStartHysteresisNumeric.Name = "anlgStartHysteresisNumeric";
            this.anlgStartHysteresisNumeric.Size = new System.Drawing.Size(137, 26);
            this.anlgStartHysteresisNumeric.TabIndex = 9;
            // 
            // anlgStartSlopeComboBox
            // 
            this.anlgStartSlopeComboBox.FormattingEnabled = true;
            this.anlgStartSlopeComboBox.Location = new System.Drawing.Point(377, 31);
            this.anlgStartSlopeComboBox.Name = "anlgStartSlopeComboBox";
            this.anlgStartSlopeComboBox.Size = new System.Drawing.Size(116, 28);
            this.anlgStartSlopeComboBox.TabIndex = 4;
            // 
            // anlgStartSlopeLabel
            // 
            this.anlgStartSlopeLabel.AutoSize = true;
            this.anlgStartSlopeLabel.Location = new System.Drawing.Point(375, 8);
            this.anlgStartSlopeLabel.Name = "anlgStartSlopeLabel";
            this.anlgStartSlopeLabel.Size = new System.Drawing.Size(54, 20);
            this.anlgStartSlopeLabel.TabIndex = 2;
            this.anlgStartSlopeLabel.Text = "Slope:";
            this.anlgStartSlopeLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // anlgStartSrcLabel
            // 
            this.anlgStartSrcLabel.AutoSize = true;
            this.anlgStartSrcLabel.Location = new System.Drawing.Point(26, 8);
            this.anlgStartSrcLabel.Name = "anlgStartSrcLabel";
            this.anlgStartSrcLabel.Size = new System.Drawing.Size(64, 20);
            this.anlgStartSrcLabel.TabIndex = 3;
            this.anlgStartSrcLabel.Text = "Source:";
            // 
            // anlgPauseTab
            // 
            this.anlgPauseTab.Controls.Add(this.anlgPauseSrcComboBox);
            this.anlgPauseTab.Controls.Add(this.anlgPauseLevelLabel);
            this.anlgPauseTab.Controls.Add(this.anlgPauseLevelNumeric);
            this.anlgPauseTab.Controls.Add(this.anlgPauseWhenComboBox);
            this.anlgPauseTab.Controls.Add(this.anlgPauseWhenLabel);
            this.anlgPauseTab.Controls.Add(this.anlgPauseSrcLabel);
            this.anlgPauseTab.Location = new System.Drawing.Point(4, 54);
            this.anlgPauseTab.Name = "anlgPauseTab";
            this.anlgPauseTab.Padding = new System.Windows.Forms.Padding(3);
            this.anlgPauseTab.Size = new System.Drawing.Size(522, 119);
            this.anlgPauseTab.TabIndex = 5;
            this.anlgPauseTab.Text = "Analog Pause";
            this.anlgPauseTab.UseVisualStyleBackColor = true;
            // 
            // anlgPauseSrcComboBox
            // 
            this.anlgPauseSrcComboBox.Location = new System.Drawing.Point(30, 32);
            this.anlgPauseSrcComboBox.Name = "anlgPauseSrcComboBox";
            this.anlgPauseSrcComboBox.Size = new System.Drawing.Size(286, 28);
            this.anlgPauseSrcComboBox.TabIndex = 18;
            this.anlgPauseSrcComboBox.Text = "APFI0";
            // 
            // anlgPauseLevelLabel
            // 
            this.anlgPauseLevelLabel.AutoSize = true;
            this.anlgPauseLevelLabel.Location = new System.Drawing.Point(26, 75);
            this.anlgPauseLevelLabel.Name = "anlgPauseLevelLabel";
            this.anlgPauseLevelLabel.Size = new System.Drawing.Size(50, 20);
            this.anlgPauseLevelLabel.TabIndex = 17;
            this.anlgPauseLevelLabel.Text = "Level:";
            // 
            // anlgPauseLevelNumeric
            // 
            this.anlgPauseLevelNumeric.DecimalPlaces = 2;
            this.anlgPauseLevelNumeric.Location = new System.Drawing.Point(82, 73);
            this.anlgPauseLevelNumeric.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.anlgPauseLevelNumeric.Minimum = new decimal(new int[] {
            1000000,
            0,
            0,
            -2147483648});
            this.anlgPauseLevelNumeric.Name = "anlgPauseLevelNumeric";
            this.anlgPauseLevelNumeric.Size = new System.Drawing.Size(137, 26);
            this.anlgPauseLevelNumeric.TabIndex = 16;
            // 
            // anlgPauseWhenComboBox
            // 
            this.anlgPauseWhenComboBox.FormattingEnabled = true;
            this.anlgPauseWhenComboBox.Location = new System.Drawing.Point(354, 32);
            this.anlgPauseWhenComboBox.Name = "anlgPauseWhenComboBox";
            this.anlgPauseWhenComboBox.Size = new System.Drawing.Size(139, 28);
            this.anlgPauseWhenComboBox.TabIndex = 15;
            // 
            // anlgPauseWhenLabel
            // 
            this.anlgPauseWhenLabel.AutoSize = true;
            this.anlgPauseWhenLabel.Location = new System.Drawing.Point(350, 9);
            this.anlgPauseWhenLabel.Name = "anlgPauseWhenLabel";
            this.anlgPauseWhenLabel.Size = new System.Drawing.Size(104, 20);
            this.anlgPauseWhenLabel.TabIndex = 13;
            this.anlgPauseWhenLabel.Text = "Pause When:";
            this.anlgPauseWhenLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // anlgPauseSrcLabel
            // 
            this.anlgPauseSrcLabel.AutoSize = true;
            this.anlgPauseSrcLabel.Location = new System.Drawing.Point(26, 9);
            this.anlgPauseSrcLabel.Name = "anlgPauseSrcLabel";
            this.anlgPauseSrcLabel.Size = new System.Drawing.Size(64, 20);
            this.anlgPauseSrcLabel.TabIndex = 14;
            this.anlgPauseSrcLabel.Text = "Source:";
            // 
            // anlgRefTab
            // 
            this.anlgRefTab.Controls.Add(this.anlgRefLabel);
            this.anlgRefTab.Location = new System.Drawing.Point(4, 54);
            this.anlgRefTab.Name = "anlgRefTab";
            this.anlgRefTab.Padding = new System.Windows.Forms.Padding(3);
            this.anlgRefTab.Size = new System.Drawing.Size(522, 119);
            this.anlgRefTab.TabIndex = 6;
            this.anlgRefTab.Text = "Analog Reference";
            this.anlgRefTab.UseVisualStyleBackColor = true;
            // 
            // anlgRefLabel
            // 
            this.anlgRefLabel.AutoSize = true;
            this.anlgRefLabel.Location = new System.Drawing.Point(38, 10);
            this.anlgRefLabel.Name = "anlgRefLabel";
            this.anlgRefLabel.Size = new System.Drawing.Size(446, 80);
            this.anlgRefLabel.TabIndex = 2;
            this.anlgRefLabel.Text = "This trigger type is not supported in continuous sample timing. \r\n\r\nRefer to your" +
    " device documentation for more information \r\non which triggers are supported.";
            // 
            // timeStartTab
            // 
            this.timeStartTab.Controls.Add(this.timeStartNote);
            this.timeStartTab.Controls.Add(this.manualTimeCheckBox);
            this.timeStartTab.Controls.Add(this.timeStartPicker);
            this.timeStartTab.Location = new System.Drawing.Point(4, 54);
            this.timeStartTab.Name = "timeStartTab";
            this.timeStartTab.Padding = new System.Windows.Forms.Padding(3);
            this.timeStartTab.Size = new System.Drawing.Size(522, 119);
            this.timeStartTab.TabIndex = 7;
            this.timeStartTab.Text = "Time Start";
            this.timeStartTab.UseVisualStyleBackColor = true;
            // 
            // timeStartNote
            // 
            this.timeStartNote.AutoSize = true;
            this.timeStartNote.Location = new System.Drawing.Point(283, 17);
            this.timeStartNote.Name = "timeStartNote";
            this.timeStartNote.Size = new System.Drawing.Size(233, 40);
            this.timeStartNote.TabIndex = 3;
            this.timeStartNote.Text = "If time is not manually set, \r\ntask will begin after 10 seconds.";
            // 
            // manualTimeCheckBox
            // 
            this.manualTimeCheckBox.AutoSize = true;
            this.manualTimeCheckBox.Location = new System.Drawing.Point(32, 22);
            this.manualTimeCheckBox.Name = "manualTimeCheckBox";
            this.manualTimeCheckBox.Size = new System.Drawing.Size(184, 24);
            this.manualTimeCheckBox.TabIndex = 1;
            this.manualTimeCheckBox.Text = "Select Time Manually";
            this.manualTimeCheckBox.UseVisualStyleBackColor = true;
            // 
            // timeStartPicker
            // 
            this.timeStartPicker.CustomFormat = "yyyy-MM-dd hh:mm:ss";
            this.timeStartPicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.timeStartPicker.Location = new System.Drawing.Point(32, 52);
            this.timeStartPicker.Name = "timeStartPicker";
            this.timeStartPicker.Size = new System.Drawing.Size(210, 26);
            this.timeStartPicker.TabIndex = 0;
            this.timeStartPicker.Value = new System.DateTime(2023, 1, 25, 10, 29, 30, 0);
            // 
            // trigParametersGroupBox
            // 
            this.trigParametersGroupBox.Controls.Add(this.trigTabControl);
            this.trigParametersGroupBox.Location = new System.Drawing.Point(353, 402);
            this.trigParametersGroupBox.Name = "trigParametersGroupBox";
            this.trigParametersGroupBox.Size = new System.Drawing.Size(562, 207);
            this.trigParametersGroupBox.TabIndex = 7;
            this.trigParametersGroupBox.TabStop = false;
            this.trigParametersGroupBox.Text = "Trigger Parameters";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(8, 19);
            this.ClientSize = new System.Drawing.Size(929, 626);
            this.Controls.Add(this.trigParametersGroupBox);
            this.Controls.Add(this.loggingParametersGroupBox);
            this.Controls.Add(this.acquisitionResultGroupBox);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.timingParametersGroupBox);
            this.Controls.Add(this.channelParametersGroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Tdms Continuous Voltage Acquisition - ScottPlot";
            this.channelParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.minimumValueNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.maximumValueNumeric)).EndInit();
            this.timingParametersGroupBox.ResumeLayout(false);
            this.timingParametersGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.rateNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.samplesPerChannelNumeric)).EndInit();
            this.acquisitionResultGroupBox.ResumeLayout(false);
            this.loggingParametersGroupBox.ResumeLayout(false);
            this.loggingParametersGroupBox.PerformLayout();
            this.trigTabControl.ResumeLayout(false);
            this.noTrigTab.ResumeLayout(false);
            this.noTrigTab.PerformLayout();
            this.digStartTab.ResumeLayout(false);
            this.digStartTab.PerformLayout();
            this.digPauseTab.ResumeLayout(false);
            this.digPauseTab.PerformLayout();
            this.digRefTab.ResumeLayout(false);
            this.digRefTab.PerformLayout();
            this.anlgStartTab.ResumeLayout(false);
            this.anlgStartTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.anlgStartLevelNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.anlgStartHysteresisNumeric)).EndInit();
            this.anlgPauseTab.ResumeLayout(false);
            this.anlgPauseTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.anlgPauseLevelNumeric)).EndInit();
            this.anlgRefTab.ResumeLayout(false);
            this.anlgRefTab.PerformLayout();
            this.timeStartTab.ResumeLayout(false);
            this.timeStartTab.PerformLayout();
            this.trigParametersGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.DoEvents();
            Application.Run(new MainForm());
        }

        private void startButton_Click(object sender, System.EventArgs e)
        {
            if (runningTask == null)
            {
                try
                {   stopButton.Enabled = true;
                    startButton.Enabled = false;
                    browseFileButton.Enabled = false;

                    // Create a new task
                    myTask = new Task();

                    // Create a virtual channel
                    myTask.AIChannels.CreateVoltageChannel(physicalChannelComboBox.Text, "",
                        (AITerminalConfiguration)(termCfgComboBox.SelectedValue), Convert.ToDouble(minimumValueNumeric.Value),
                        Convert.ToDouble(maximumValueNumeric.Value), AIVoltageUnits.Volts);

                    // Configure the timing parameters
                    myTask.Timing.ConfigureSampleClock(clkSrcComboBox.Text, Convert.ToDouble(rateNumeric.Value),
                        SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 1000);

                    // Configure the trigger parameters
                    switch (trigTabControl.SelectedTab.Name)
                    {
                        case "noTrigTab":
                        case "digRefTab":
                            break;
                        case "digStartTab":
                            myTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger(digStartSrcComboBox.Text, 
                                (DigitalEdgeStartTriggerEdge)(digStartEdgeComboBox.SelectedValue));
                            break;
                        case "digPauseTab":
                            myTask.Triggers.PauseTrigger.ConfigureDigitalLevelTrigger(digPauseSrcComboBox.Text, 
                                (DigitalLevelPauseTriggerCondition)(digPauseWhenComboBox.SelectedValue));
                            break;
                        case "anlgStartTab":
                            myTask.Triggers.StartTrigger.ConfigureAnalogEdgeTrigger(anlgStartSrcComboBox.Text, 
                                (AnalogEdgeStartTriggerSlope)(anlgStartSlopeComboBox.SelectedValue), Convert.ToDouble(anlgStartLevelNumeric.Value));
                            myTask.Triggers.StartTrigger.AnalogEdge.Hysteresis = Convert.ToDouble(anlgStartHysteresisNumeric.Value);
                            break;
                        case "anlgPauseTab":
                            myTask.Triggers.PauseTrigger.ConfigureAnalogLevelTrigger(anlgPauseSrcComboBox.Text,
                                (AnalogLevelPauseTriggerCondition)(anlgPauseWhenComboBox.SelectedValue), Convert.ToDouble(anlgPauseLevelNumeric.Value));
                            break;
                        case "timeStartTab":
                            DateTime timeStart = new DateTime();
                            if (manualTimeCheckBox.Checked == true)
                                timeStart = timeStartPicker.Value;
                            else
                                timeStart = (DateTime.Now).AddSeconds(10);

                            PrecisionDateTime precisionStartTime = new PrecisionDateTime(timeStart);
                            myTask.Triggers.StartTrigger.ConfigureTimeTrigger(precisionStartTime, TimeStartTriggerTimescale.HostTime);
                            break;
                    }

                    // Configure TDMS Logging
                    if (tdmsFilePathTextBox.Text.Trim().Length > 0)
                        myTask.ConfigureLogging(tdmsFilePathTextBox.Text, TdmsLoggingOperation.CreateOrReplace, 
                            LoggingMode.LogAndRead, "Group Name");

                    // Verify the Task
                    myTask.Control(TaskAction.Verify);

                    runningTask = myTask;
                    analogInReader = new AnalogMultiChannelReader(myTask.Stream);
                    analogCallback = new AsyncCallback(AnalogInCallback);

                    // Use SynchronizeCallbacks to specify that the object 
                    // marshals callbacks across threads appropriately.
                    analogInReader.SynchronizeCallbacks = true;
                    analogInReader.BeginReadWaveform(Convert.ToInt32(samplesPerChannelNumeric.Value), 
                        analogCallback, myTask);
                    actualRateTextBox.Text = $"{myTask.Timing.SampleClockRate}";
                }
                catch (DaqException exception)
                {
                    // Display Errors
                    MessageBox.Show(exception.Message);
                    runningTask = null;
                    myTask.Dispose();
                    stopButton.Enabled = false;
                    startButton.Enabled = true;
                    browseFileButton.Enabled = true;
                }
            }
        }

        private void AnalogInCallback(IAsyncResult ar)
        {
            try
            {
                if (runningTask != null && runningTask == ar.AsyncState)
                {
                    // Read the available data from the channels
                    data = analogInReader.EndReadWaveform(ar);

                    // Plot your data here
                    dataToPlot(data, ref acqPlot);

                    analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(samplesPerChannelNumeric.Value),
                        analogCallback, myTask, data);
                }
            }
            catch (DaqException exception)
            {
                // Display Errors
                MessageBox.Show(exception.Message);
                runningTask = null;
                myTask.Dispose();
                stopButton.Enabled = false;
                startButton.Enabled = true;
            }
        }

        private void stopButton_Click(object sender, System.EventArgs e)
        {
            if (runningTask != null)
            {
                // Dispose of the task
                runningTask = null;
                myTask.Dispose();
                stopButton.Enabled = false;
                startButton.Enabled = true;
                browseFileButton.Enabled = true;
            }
        }

        private void browseButton_Click(object sender, System.EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                InitialDirectory = @"C:\",
                Title = "Browse TDMS File",

                CheckFileExists = false,
                CheckPathExists = true,

                DefaultExt = "tdms",
                Filter = "TDMS files (*.tdms)|*.tdms",
                FilterIndex = 2,
                RestoreDirectory = true,
            };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                tdmsFilePathTextBox.Text = openFileDialog1.FileName;
            }
        }

        private void dataToPlot(AnalogWaveform<double>[] sourceArray, ref FormsPlot dataPlot)
        {
            // Get paramaters from waveform
            DateTime t0 = sourceArray[0].Timing.TimeStamp;
            // The value of sample rate is the number of samples per day
            double sampleRatePlot = 1 / (sourceArray[0].Timing.SampleInterval.TotalDays);
            int sampleCount = sourceArray[0].SampleCount;
            var values = new double[sampleCount];
            var plt = dataPlot.Plot;
            plt.Clear();
            plt.Title("Acquired Data");
            // indicate the horizontal axis tick labels should display DateTime units
            plt.XAxis.DateTimeFormat(true);

            // Iterate over channels
            int currentLineIndex = 0;
            foreach (AnalogWaveform<double> waveform in sourceArray)
            {
                values = waveform.GetScaledData();
                var sig = plt.AddSignal(values, sampleRatePlot, label: waveform.ChannelName);
                sig.OffsetX = t0.ToOADate();

                currentLineIndex++;
            }

            dataPlot.Refresh();
        }

        private void browseChanButton_Click(object sender, EventArgs e)
        {
            phyChanPopupForm phyChanPopupForm = new phyChanPopupForm();
            if (phyChanPopupForm.ShowDialog() == DialogResult.OK)
            {
                physicalChannelComboBox.Text = phyChanPopupForm.phyChannels;
            }
        }
    }
}
