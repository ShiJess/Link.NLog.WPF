using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using NLog.Common;
using NLog.Config;
using NLog.Targets;

namespace NLog.WPF
{
    /// <summary>
    /// Log text a Rich Text Box control in an existing or new form.
    /// </summary>  
    [Target("RichTextBox")]
    public sealed class RichTextBoxTarget : TargetWithLayout
    {
        /// <summary>
        /// Initializes static members of the RichTextBoxTarget class.
        /// </summary>
        /// <remarks>
        /// The default value of the layout is: <code>${longdate}|${level:uppercase=true}|${logger}|${message}</code>
        /// </remarks>
        static RichTextBoxTarget()
        {
            var rules = new List<RichTextBoxRowColoringRule>()
            {
                new RichTextBoxRowColoringRule("level == LogLevel.Fatal", "White", "Red", FontStyles.Normal,FontWeights.Bold),
                new RichTextBoxRowColoringRule("level == LogLevel.Error", "Red", "Empty",FontStyles.Italic, FontWeights.Bold ),
                //new RichTextBoxRowColoringRule("level == LogLevel.Warn", "Orange", "Empty", FontStyle.Underline),
                new RichTextBoxRowColoringRule("level == LogLevel.Info", "Black", "Empty"),
                new RichTextBoxRowColoringRule("level == LogLevel.Debug", "Gray", "Empty"),
                new RichTextBoxRowColoringRule("level == LogLevel.Trace", "DarkGray", "Empty", FontStyles.Italic,FontWeights.Normal),
            };

            DefaultRowColoringRules = rules.AsReadOnly();
        }

        /// <summary>
        /// Attempts to attach existing targets that have yet no textboxes to controls that exist on specified form if appropriate
        /// </summary>
        /// <remarks>
        /// Setting <see cref="AllowAccessoryFormCreation"/> to true (default) actually causes target to always have a textbox 
        /// (after having <see cref="InitializeTarget"/> called), so such targets are not affected by this method.
        /// </remarks>
        /// <param name="form">a Form to check for RichTextBoxes</param>
        public static void ReInitializeAllTextboxes(Window form)
        {
            InternalLogger.Info("Executing ReInitializeAllTextboxes for Form {0}", form);
            foreach (Target target in LogManager.Configuration.AllTargets)
            {
                RichTextBoxTarget textboxTarget = target as RichTextBoxTarget;
                if (textboxTarget != null && textboxTarget.FormName == form.Name)
                {
                    //can't use InitializeTarget here as the Application.OpenForms would not work from Form's constructor
                    RichTextBox textboxControl = form.FindName(textboxTarget.ControlName) as RichTextBox; //FormHelper.FindControl<RichTextBox>(textboxTarget.ControlName, form);
                    if (textboxControl != null)//&& !textboxControl.IsDisposed)
                    {
                        if (textboxTarget.TargetRichTextBox == null
                            //|| textboxTarget.TargetRichTextBox.IsDisposed
                            || textboxTarget.TargetRichTextBox != textboxControl
                        )
                        {
                            textboxTarget.AttachToControl(form, textboxControl);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a target attached to a given RichTextBox control
        /// </summary>
        /// <param name="control">a RichTextBox control for which the target is to be returned</param>
        /// <returns>A RichTextBoxTarget attached to a given control or <code>null</code> if no target is attached</returns>
        public static RichTextBoxTarget GetTargetByControl(RichTextBox control)
        {
            foreach (Target target in LogManager.Configuration.AllTargets)
            {
                RichTextBoxTarget textboxTarget = target as RichTextBoxTarget;
                if (textboxTarget != null && textboxTarget.TargetRichTextBox == control)
                {
                    return textboxTarget;
                }
            }
            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RichTextBoxTarget" /> class.
        /// </summary>
        /// <remarks>
        /// The default value of the layout is: <code>${longdate}|${level:uppercase=true}|${logger}|${message}</code>
        /// </remarks>
        public RichTextBoxTarget()
        {
            WordColoringRules = new List<RichTextBoxWordColoringRule>();
            RowColoringRules = new List<RichTextBoxRowColoringRule>();
            ToolWindow = true;
            AllowAccessoryFormCreation = true;
        }

        private delegate void DelSendTheMessageToRichTextBox(string logMessage, RichTextBoxRowColoringRule rule, LogEventInfo logEvent);

        private delegate void FormCloseDelegate();

        /// <summary>
        /// Gets the default set of row coloring rules which applies when <see cref="UseDefaultRowColoringRules"/> is set to true.
        /// </summary>
        public static ReadOnlyCollection<RichTextBoxRowColoringRule> DefaultRowColoringRules { get; private set; }

        /// <summary>
        /// Gets or sets the Name of RichTextBox to which Nlog will write.
        /// </summary>
        /// <docgen category='Form Options' order='10' />
        public string ControlName { get; set; }

        /// <summary>
        /// Gets or sets the name of the Form on which the control is located. 
        /// If there is no open form of a specified name than NLog will create a new one.
        /// </summary>
        /// <docgen category='Form Options' order='10' />
        public string FormName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use default coloring rules.
        /// </summary>
        /// <docgen category='Highlighting Options' order='10' />
        [DefaultValue(false)]
        public bool UseDefaultRowColoringRules { get; set; }

        /// <summary>
        /// Gets the row coloring rules.
        /// </summary>
        /// <docgen category='Highlighting Options' order='10' />
        [ArrayParameter(typeof(RichTextBoxRowColoringRule), "row-coloring")]
        public IList<RichTextBoxRowColoringRule> RowColoringRules { get; private set; }

        /// <summary>
        /// Gets the word highlighting rules.
        /// </summary>
        /// <docgen category='Highlighting Options' order='10' />
        [ArrayParameter(typeof(RichTextBoxWordColoringRule), "word-coloring")]
        public IList<RichTextBoxWordColoringRule> WordColoringRules { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the created window will be a tool window.
        /// </summary>
        /// <remarks>
        /// This parameter is ignored when logging to existing form control.
        /// Tool windows have thin border, and do not show up in the task bar.
        /// </remarks>
        /// <docgen category='Form Options' order='10' />
        [DefaultValue(true)]
        public bool ToolWindow { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the created form will be initially minimized.
        /// </summary>
        /// <remarks>
        /// This parameter is ignored when logging to existing form control.
        /// </remarks>
        /// <docgen category='Form Options' order='10' />
        public bool ShowMinimized { get; set; }

        /// <summary>
        /// Gets or sets the initial width of the form with rich text box.
        /// </summary>
        /// <remarks>
        /// This parameter is ignored when logging to existing form control.
        /// </remarks>
        /// <docgen category='Form Options' order='10' />
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the initial height of the form with rich text box.
        /// </summary>
        /// <remarks>
        /// This parameter is ignored when logging to existing form control.
        /// </remarks>
        /// <docgen category='Form Options' order='10' />
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether scroll bar will be moved automatically to show most recent log entries.
        /// </summary>
        /// <docgen category='Form Options' order='10' />
        public bool AutoScroll { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of lines the rich text box will store (or 0 to disable this feature).
        /// </summary>
        /// <remarks>
        /// After exceeding the maximum number, first line will be deleted. 
        /// </remarks>
        /// <docgen category='Form Options' order='10' />
        public int MaxLines { get; set; }

        /// <summary>
        /// Gets or sets the form to log to.
        /// </summary>
        public Window TargetForm { get; set; }

        /// <summary>
        /// Gets or sets the rich text box to log to.
        /// </summary>
        public RichTextBox TargetRichTextBox { get; set; }

        /// <summary>
        /// Form created (true) or used an existing (false). Set after <see cref="InitializeTarget"/>. Can be true only if <see cref="AllowAccessoryFormCreation"/> is set to true (default).
        /// </summary>
        public bool CreatedForm { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to create accessory form if the specified form/control combination was not found during target initialization.
        /// </summary>
        /// <remarks>
        /// If set to false and the control was not found during target initialiation, the target would skip events until the control is found during <see cref="ReInitializeAllTextboxes"/> call
        /// </remarks>
        /// <docgen category='Form Options' order='10' />
        [DefaultValue(true)]
        public bool AllowAccessoryFormCreation { get; set; }


        /// <summary>
        /// gets or sets the message retention strategy which determines how the target handles messages when there's no control attached, or when switching between controls
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <docgen category='Form Options' order='10' />
        [DefaultValue(RichTextBoxTargetMessageRetentionStrategy.None)]
        public RichTextBoxTargetMessageRetentionStrategy MessageRetention
        {
            get { return messageRetention; }
            set
            {
                lock (messageQueueLock)
                {
                    messageRetention = value;
                    if (messageRetention == RichTextBoxTargetMessageRetentionStrategy.None)
                    {
                        messageQueue = null;
                    }
                    else
                    {
                        if (MaxLines <= 0)
                        {
                            HandleError("Forbidden usage of RetentionStrategy ({0}) when MaxLines is not set", value);
                        }
                        if (messageQueue == null)
                        {
                            messageQueue = new Queue<MessageInfo>();    //no need to use MaxLine here, it could cause unnecessary memory allocation for huge limits
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Actual value of the <see cref="MessageRetention"/>.
        /// </summary>
        private RichTextBoxTargetMessageRetentionStrategy messageRetention = RichTextBoxTargetMessageRetentionStrategy.None;

        /// <summary>
        /// A textbox to which we have logged last time. Used to prevent duplicating messages in the same textbox in case of config reload and RichTextBoxTargetMessageRetentionStrategy.All
        /// see https://github.com/NLog/NLog.Windows.Forms/pull/22
        /// </summary>
        private RichTextBox lastLoggedTextBoxControl;

        /// <summary>
        /// a lock object used to synchronize access to <see cref="messageQueue"/>
        /// </summary>
        private readonly object messageQueueLock = new object();

        /// <summary>
        /// A queue used to store messages based on <see cref="MessageRetention"/>.
        /// </summary>
        private volatile Queue<MessageInfo> messageQueue;

        /// <summary>
        /// If set to true, using "rtb-link" renderer (<see cref="RichTextBoxLinkLayoutRenderer"/>) would create clickable links in the control.
        /// <seealso cref="LinkClicked"/>
        /// </summary>
        [DefaultValue(false)]
        public bool SupportLinks
        {
            get { return supportLinks; }
            set
            {
                if (value)
                {
                    lock (linkRegexLock)
                    {
                        if (linkAddRegex == null)
                        {
                            linkAddRegex = new Regex(@"(\([a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12}\))", RegexOptions.Compiled);
                            linkRemoveRtfRegex = new Regex(@"\\v #" + LinkPrefix + @"(\d+)\\v0", RegexOptions.Compiled);
                        }
                    }
                    lock (linkedEventsLock)
                    {
                        if (linkedEvents == null)
                        {
                            linkedEvents = new Dictionary<int, LogEventInfo>();
                        }
                    }
                }
                //update the field value after regex initialization to prevent concurrent access to not-initialized fields.
                supportLinks = value;
            }
        }

        /// <summary>
        /// Type of delegate for <see cref="LinkClicked"/> event.
        /// </summary>
        /// <param name="sender">The target that caused the event</param>
        /// <param name="linkText">Visible text of the link being clicked</param>
        /// <param name="logEvent">Original log event that caused a line with the link</param>
        public delegate void DelLinkClicked(RichTextBoxTarget sender, string linkText, LogEventInfo logEvent);

        /// <summary>
        /// Event fired when the user clicks on a link in the control created by the "rtb-link" renderer (<see cref="RichTextBoxLinkLayoutRenderer"/>).
        /// <seealso cref="DelLinkClicked"/>
        /// </summary>
        public event DelLinkClicked LinkClicked;

        /// <summary>
        /// Actual value of the <see cref="LinkClicked"/> property
        /// </summary>
        private bool supportLinks;

        /// <summary>
        /// Lock for <see cref="linkedEvents"/> dictionary access
        /// </summary>
        private readonly object linkedEventsLock = new object();

        /// <summary>
        /// A map from link id to a corresponding log event
        /// </summary>
        private Dictionary<int, LogEventInfo> linkedEvents;

        /// <summary>
        /// Returns number of events stored for active links in the control. 
        /// Used only in tests to check that non needed events are removed.
        /// </summary>
        internal int? LinkedEventsCount
        {
            get
            {
                lock (linkedEventsLock)
                {
                    if (linkedEvents == null)
                    {
                        return null;
                    }
                    return linkedEvents.Count;
                }
            }
        }

        /// <summary>
        /// Internal prefix that is added to the link id in RTF
        /// </summary>
        private const string LinkPrefix = "link";

        /// <summary>
        /// Used to synchronize lazy initialization of <see cref="linkAddRegex"/> and <see cref="linkRemoveRtfRegex"/> in <see cref="SupportLinks"/>.set
        /// </summary>
        private static readonly object linkRegexLock = new object();

        /// <summary>
        /// Used to capture link placeholders in <see cref="SendTheMessageToRichTextBox"/>
        /// Lazily initialized in <see cref="SupportLinks"/>.set(true). Assure checking <see cref="SupportLinks"/> before accessing the field 
        /// </summary>
        private static Regex linkAddRegex;

        /// <summary>
        /// Used to parse RTF with links when removing excess lines in <see cref="SendTheMessageToRichTextBox"/>
        /// Lazily initialized in <see cref="SupportLinks"/>.set(true). Assure checking <see cref="SupportLinks"/> before accessing the field
        /// </summary>
        private static Regex linkRemoveRtfRegex;


        /// <summary>
        /// Initializes the target. Can be used by inheriting classes
        /// to initialize logging.
        /// </summary>
        protected override void InitializeTarget()
        {
            base.InitializeTarget();

            if (TargetRichTextBox != null)
            {
                //already initialized by ReInitializeAllTextboxes call
                return;
            }

            CreatedForm = false;
            Window openFormByName;
            RichTextBox targetControl;

            if (AllowAccessoryFormCreation)
            {
                //old behaviour which causes creation of accessory form in case specified control cannot be found on specified form

                if (FormName == null)
                {
                    InternalLogger.Info("FormName not set, creating acceccory form");
                    CreateAccessoryForm();
                    return;
                }

                openFormByName = (from c in Application.Current.Windows.Cast<Window>() where c.Name == FormName select c).FirstOrDefault(); //Application.OpenForms[FormName];
                if (openFormByName == null)
                {
                    InternalLogger.Info("Form {0} not found, creating accessory form", FormName);
                    CreateAccessoryForm();
                    return;
                }

                if (string.IsNullOrEmpty(ControlName))
                {
                    HandleError("Rich text box control name must be specified for {0}.", GetType().Name);
                    CreateAccessoryForm();
                    return;
                }

                targetControl = openFormByName.FindName(ControlName) as RichTextBox; //FormHelper.FindControl<RichTextBox>(ControlName, openFormByName);
                if (targetControl == null)
                {
                    HandleError("Rich text box control '{0}' cannot be found on form '{1}'.", ControlName, FormName);
                    CreateAccessoryForm();
                    return;
                }

                //finally attached to proper control
            }
            else
            {
                //new behaviour which postpones attaching to textbox if it's not yet available at the time,

                if (FormName == null)
                {
                    HandleError("FormName should be specified for {0}.{1}", GetType().Name, this.Name);
                    return;
                }

                if (string.IsNullOrEmpty(ControlName))
                {
                    HandleError("Rich text box control name must be specified for {0}.{1}", GetType().Name, this.Name);
                    return;
                }

                openFormByName = (from c in Application.Current.Windows.Cast<Window>() where c.Name == FormName select c).FirstOrDefault();// Application.OpenForms[FormName];
                if (openFormByName == null)
                {
                    InternalLogger.Info("Form {0} not found, waiting for ReInitializeAllTextboxes.", FormName);
                    return;
                }

                targetControl = openFormByName.FindName(ControlName) as RichTextBox;// FormHelper.FindControl<RichTextBox>(ControlName, openFormByName);
                if (targetControl == null)
                {
                    InternalLogger.Info("Rich text box control '{0}' cannot be found on form '{1}'. Waiting for ReInitializeAllTextboxes.", ControlName, FormName);
                    return;
                }

                //actually attached to a target, all ok
            }

            AttachToControl(openFormByName, targetControl);
        }

        /// <summary>
        /// Called from constructor when error is detected. In case LogManager.ThrowExceptions is enabled, throws the exception, otherwise - logs the problem message
        /// </summary>
        /// <param name="message">exception/log message format</param>
        /// <param name="args">message format arguments</param>
        private static void HandleError(string message, params object[] args)
        {
            if (LogManager.ThrowExceptions)
            {
                throw new NLogConfigurationException(String.Format(message, args));
            }
            InternalLogger.Error(message, args);
        }

        /// <summary>
        /// Used to create accessory form with textbox in case specified form or control were not found during InitializeTarget() and AllowAccessoryFormCreation==true
        /// </summary>
        private void CreateAccessoryForm()
        {
            if (FormName == null)
            {
                FormName = "NLogForm" + Guid.NewGuid().ToString("N");
            }
            Window form = CreateForm(FormName, Width, Height, true, ShowMinimized, ToolWindow);
            RichTextBox control = form.FindName(ControlName) as RichTextBox; //FormHelper.CreateRichTextBox(ControlName, form);
            AttachToControl(form, control);
            CreatedForm = true;
        }

        /// <summary>
        /// Used to (re)initialize target when attaching it to another RTB control
        /// </summary>
        /// <param name="form">form owning textboxControl</param>
        /// <param name="textboxControl">a new control to attach to</param>
        private void AttachToControl(Window form, RichTextBox textboxControl)
        {
            InternalLogger.Info("Attaching target {0} to textbox {1}.{2}", this.Name, form.Name, textboxControl.Name);
            DetachFromControl();
            this.TargetForm = form;
            this.TargetRichTextBox = textboxControl;

            if (this.SupportLinks)
            {
                //this.TargetRichTextBox.DetectUrls = false;
                //this.TargetRichTextBox.LinkClicked += TargetRichTextBox_LinkClicked;                
                this.TargetRichTextBox.AddHandler(System.Windows.Documents.Hyperlink.ClickEvent, new System.Windows.RoutedEventHandler(this.TargetRichTextBox_LinkClicked));
            }

            //OnReattach?
            switch (messageRetention)
            {
                case RichTextBoxTargetMessageRetentionStrategy.None:
                    break;
                case RichTextBoxTargetMessageRetentionStrategy.All:
                    if (lastLoggedTextBoxControl != textboxControl) //don't log to same RTB as befre, because it already has everything. Happens in case of config reload, see https://github.com/NLog/NLog.Windows.Forms/pull/22
                    {
                        lock (messageQueueLock)
                        {
                            foreach (MessageInfo messageInfo in messageQueue)
                            {
                                DoSendMessageToTextbox(messageInfo.Message, messageInfo.Rule, messageInfo.LogEvent);
                            }
                        }
                    }
                    break;
                case RichTextBoxTargetMessageRetentionStrategy.OnlyMissed:
                    lock (messageQueueLock)
                    {
                        while (messageQueue.Count > 0)
                        {
                            MessageInfo messageInfo = messageQueue.Dequeue();
                            DoSendMessageToTextbox(messageInfo.Message, messageInfo.Rule, messageInfo.LogEvent);
                        }
                    }
                    break;
                default:
                    HandleError("Unexpected retention strategy {0}", messageRetention);
                    break;
            }
        }

        /// <summary>
        /// Attached to RTB-control's LinkClicked event to convert link text to logEvent
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TargetRichTextBox_LinkClicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var linktext = (e.OriginalSource as Hyperlink).NavigateUri.ToString();


            DelLinkClicked linkClickEvent = LinkClicked;
            if (linkClickEvent != null)
            {
                string linkText = linktext;//.Substring(0, match.Index);
                linkClickEvent(this, linkText, null);
            }


            //Match match = Regex.Match(linktext, "#" + LinkPrefix + @"(\d+)");
            //if (!match.Success)
            //{
            //    //could be a link inserted by another RTB control user
            //    InternalLogger.Warn("Unexpected link format '{0}', skipping", linktext);
            //    return;
            //}

            //int id;
            //if (!int.TryParse(match.Groups[1].Value, out id))
            //{
            //    //still could be a link inserted by another RTB control user
            //    InternalLogger.Warn("Unexpected link format '{0}', skipping", linktext);
            //    return;
            //}

            //LogEventInfo logEvent;
            //lock (linkedEventsLock)
            //{
            //    linkedEvents.TryGetValue(id, out logEvent);
            //}
            //if (logEvent == null)
            //{
            //    HandleError("Missing link id {0}", id);
            //    return;
            //}

            //DelLinkClicked linkClickEvent = LinkClicked;
            //if (linkClickEvent != null)
            //{
            //    string linkText = linktext.Substring(0, match.Index);
            //    linkClickEvent(this, linkText, logEvent);
            //}
        }

        /// <summary>
        /// if <see cref="CreatedForm"/> is true, then destroys created form. Resets <see cref="CreatedForm"/>, <see cref="TargetForm"/> and <see cref="TargetRichTextBox"/> to default values
        /// </summary>
        private void DetachFromControl()
        {
            if (CreatedForm)
            {
                try
                {
                    //if (!TargetForm.IsDisposed)
                    //{
                    //    if (TargetForm.InvokeRequired)
                    //    {
                    //        TargetForm.BeginInvoke((FormCloseDelegate)TargetForm.Close);
                    //    }
                    //    else
                    //    {
                    //        TargetForm.Close();
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    InternalLogger.Warn(ex.ToString());

                    if (LogManager.ThrowExceptions)
                    {
                        throw;
                    }
                }
                CreatedForm = false;
            }
            TargetForm = null;
            TargetRichTextBox = null;
        }

        /// <summary>
        /// Closes the target and releases any unmanaged resources.
        /// </summary>
        protected override void CloseTarget()
        {
            DetachFromControl();
        }

        /// <summary>
        /// Log message to RichTextBox.
        /// </summary>
        /// <param name="logEvent">The logging event.</param>
        protected override void Write(LogEventInfo logEvent)
        {
            RichTextBox textbox = TargetRichTextBox;
            if (textbox == null)// || textbox.IsDisposed)
            {
                //no last logged textbox
                lastLoggedTextBoxControl = null;
                if (AllowAccessoryFormCreation)
                {
                    CreateAccessoryForm();
                }
                else if (messageRetention == RichTextBoxTargetMessageRetentionStrategy.None)
                {
                    InternalLogger.Trace("Textbox for target {0} is {1}, skipping logging", this.Name, textbox == null ? "null" : "disposed");
                    return;
                }
            }

            string logMessage = Layout.Render(logEvent);
            RichTextBoxRowColoringRule matchingRule = FindMatchingRule(logEvent);

            bool messageSent = DoSendMessageToTextbox(logMessage, matchingRule, logEvent);

            if (messageSent)
            {
                //remember last logged text box
                lastLoggedTextBoxControl = textbox;
            }

            switch (messageRetention)
            {
                case RichTextBoxTargetMessageRetentionStrategy.None:
                    break;
                case RichTextBoxTargetMessageRetentionStrategy.All:
                    StoreMessage(logMessage, matchingRule, logEvent);
                    break;
                case RichTextBoxTargetMessageRetentionStrategy.OnlyMissed:
                    if (!messageSent)
                    {
                        StoreMessage(logMessage, matchingRule, logEvent);
                    }
                    break;
                default:
                    HandleError("Unexpected retention strategy {0}", messageRetention);
                    break;
            }
        }

        /// <summary>
        /// Actually sends log message to <see cref="TargetRichTextBox"/>
        /// </summary>
        /// <param name="logMessage">a message to send</param>
        /// <param name="rule">matching coloring rule</param>
        /// <param name="logEvent">original logEvent</param>
        /// <returns>true if the message was actually sent (i.e. <see cref="TargetRichTextBox"/> is not null and not disposed, and no exception happened during message send)</returns>
        private bool DoSendMessageToTextbox(string logMessage, RichTextBoxRowColoringRule rule, LogEventInfo logEvent)
        {
            RichTextBox textbox = TargetRichTextBox;
            try
            {
                if (textbox != null)//&& !textbox.IsDisposed)
                {
                    //if (textbox.InvokeRequired)
                    //{
                    //    textbox.BeginInvoke(new DelSendTheMessageToRichTextBox(SendTheMessageToRichTextBox), logMessage, rule, logEvent);
                    //}
                    //else
                    //{
                    SendTheMessageToRichTextBox(logMessage, rule, logEvent);
                    //}
                    return true;
                }
            }
            catch (Exception ex)
            {
                InternalLogger.Warn(ex.ToString());

                if (LogManager.ThrowExceptions)
                {
                    throw;
                }
            }
            return false;
        }

        /// <summary>
        /// Find first matching rule
        /// </summary>
        /// <param name="logEvent"></param>
        /// <returns></returns>
        private RichTextBoxRowColoringRule FindMatchingRule(LogEventInfo logEvent)
        {
            //custom rules first
            if (RowColoringRules != null)
            {
                foreach (RichTextBoxRowColoringRule coloringRule in RowColoringRules)
                {
                    if (coloringRule.CheckCondition(logEvent))
                    {
                        return coloringRule;
                    }
                }
            }

            if (UseDefaultRowColoringRules && DefaultRowColoringRules != null)
            {
                foreach (RichTextBoxRowColoringRule coloringRule in DefaultRowColoringRules)
                {
                    if (coloringRule.CheckCondition(logEvent))
                    {
                        return coloringRule;
                    }
                }
            }

            return RichTextBoxRowColoringRule.Default;
        }

        private static Color GetColorFromString(string color, Brush defaultColor)
        {
            //if (color == "Empty")
            //{
            //    return defaultColor;
            //}
            if (color == "Empty")
            {
                color = "White";
            }
            return (Color)colorConverter.ConvertFromString(color);
            //return Color.FromName(color);
        }

        private void SendTheMessageToRichTextBox(string logMessage, RichTextBoxRowColoringRule rule, LogEventInfo logEvent)
        {
            RichTextBox textBox = TargetRichTextBox;
            //BlockUIContainer block = new BlockUIContainer();
            Run run = new Run(logMessage + "\n", textBox.Document.ContentEnd);
            run.Foreground = new SolidColorBrush(Colors.White);
            Hyperlink hyperlink = new Hyperlink(run);
            hyperlink.NavigateUri = new Uri("http://www.baidu.com");
            //hyperlink.NavigateUri = new Uri(logMessage);
            //Paragraph myParagraph = new Paragraph(run);
            Paragraph myParagraph = new Paragraph(hyperlink);
            myParagraph.Margin = new Thickness(0);
            myParagraph.Padding = new Thickness(0);
            //myParagraph.Inlines.Add(run);
            textBox.Document.Blocks.Add(myParagraph);

            //int startIndex = textBox.Text.Length;
            //textBox.SelectionStart = startIndex;
            //textBox.SelectionBackColor = GetColorFromString(rule.BackgroundColor, textBox.BackColor);
            //textBox.SelectionColor = GetColorFromString(rule.FontColor, textBox.ForeColor);
            //textBox.SelectionFont = new Font(textBox.SelectionFont, textBox.SelectionFont.Style ^ rule.Style);
            //textBox.AppendText(logMessage + "\n");
            //textBox.SelectionLength = textBox.Text.Length - textBox.SelectionStart;

            //// find word to color
            //foreach (RichTextBoxWordColoringRule wordRule in WordColoringRules)
            //{
            //    MatchCollection matches = wordRule.CompiledRegex.Matches(textBox.Text, startIndex);
            //    foreach (Match match in matches)
            //    {
            //        textBox.SelectionStart = match.Index;
            //        textBox.SelectionLength = match.Length;
            //        textBox.SelectionBackColor = GetColorFromString(wordRule.BackgroundColor, textBox.BackColor);
            //        textBox.SelectionColor = GetColorFromString(wordRule.FontColor, textBox.ForeColor);
            //        textBox.SelectionFont = new Font(textBox.SelectionFont, textBox.SelectionFont.Style ^ wordRule.Style);
            //    }
            //}

            //if (SupportLinks)
            //{
            //    object linkInfoObj;
            //    lock (logEvent.Properties)
            //    {
            //        logEvent.Properties.TryGetValue(RichTextBoxLinkLayoutRenderer.LinkInfo.PropertyName, out linkInfoObj);
            //    }
            //    if (linkInfoObj != null)
            //    {
            //        RichTextBoxLinkLayoutRenderer.LinkInfo linkInfo = (RichTextBoxLinkLayoutRenderer.LinkInfo)linkInfoObj;

            //        bool linksAdded = false;

            //        textBox.SelectionStart = startIndex;
            //        textBox.SelectionLength = textBox.Text.Length - textBox.SelectionStart;
            //        string addedText = textBox.SelectedText;
            //        MatchCollection matches = linkAddRegex.Matches(addedText); //only access regex after checking SupportLinks, as it assures the initialization
            //        for (int i = matches.Count - 1; i >= 0; --i)    //backwards order, so the string positions are not affected
            //        {
            //            Match match = matches[i];
            //            string linkText = linkInfo.GetValue(match.Value);
            //            if (linkText != null)
            //            {
            //                textBox.SelectionStart = startIndex + match.Index;
            //                textBox.SelectionLength = match.Length;
            //                FormHelper.ChangeSelectionToLink(textBox, linkText, LinkPrefix + logEvent.SequenceID);
            //                linksAdded = true;
            //            }
            //        }
            //        if (linksAdded)
            //        {
            //            linkedEvents[logEvent.SequenceID] = logEvent;
            //        }
            //    }
            //}

            ////remove some lines if there above the max
            //if (MaxLines > 0)
            //{
            //    //find the last line by reading the textbox
            //    var lastLineWithContent = textBox.Lines.LastOrDefault(f => !string.IsNullOrEmpty(f));
            //    if (lastLineWithContent != null)
            //    {
            //        char lastChar = lastLineWithContent.Last();
            //        var visibleLineCount = textBox.GetLineFromCharIndex(textBox.Text.LastIndexOf(lastChar));
            //        var tooManyLines = (visibleLineCount - MaxLines) + 1;
            //        if (tooManyLines > 0)
            //        {
            //            textBox.SelectionStart = 0;
            //            textBox.SelectionLength = textBox.GetFirstCharIndexFromLine(tooManyLines);
            //            if (SupportLinks)
            //            {
            //                string selectedRtf = textBox.SelectedRtf;
            //                //only access regex after checking SupportLinks, as it assures the initialization
            //                foreach (Match match in linkRemoveRtfRegex.Matches(selectedRtf))
            //                {
            //                    int id;
            //                    if (int.TryParse(match.Groups[1].Value, out id))
            //                    {
            //                        lock (linkedEventsLock)
            //                        {
            //                            linkedEvents.Remove(id);
            //                        }
            //                    }
            //                }
            //            }
            //            textBox.SelectedRtf = "{\\rtf1\\ansi}";
            //        }
            //    }
            //}

            //if (AutoScroll)
            //{
            //    textBox.Select(textBox.TextLength, 0);
            //    textBox.ScrollToCaret();
            //}





            System.Windows.Controls.RichTextBox rtbx = TargetRichTextBox;

            var tr = new TextRange(rtbx.Document.ContentEnd, rtbx.Document.ContentEnd);
            tr.Text = logMessage + "\n";
            tr.ApplyPropertyValue(TextElement.ForegroundProperty,
                new SolidColorBrush(GetColorFromString(rule.FontColor, (Brush)tr.GetPropertyValue(TextElement.ForegroundProperty)))
            );
            tr.ApplyPropertyValue(TextElement.BackgroundProperty,
                new SolidColorBrush(GetColorFromString(rule.BackgroundColor, (Brush)tr.GetPropertyValue(TextElement.BackgroundProperty)))
            );
            tr.ApplyPropertyValue(TextElement.FontStyleProperty, rule.Style);
            tr.ApplyPropertyValue(TextElement.FontWeightProperty, rule.Weight);


            //tr.ApplyPropertyValue(Inline.TextDecorationsProperty,TextDecorations.Baseline);
            //tr.ApplyPropertyValue(Hyperlink.NavigateUriProperty, new Uri("http://www.baidu.com"));

            //if (this.MaxLines > 0)
            //{
            //    this.lineCount++;
            //    if (this.lineCount > MaxLines)
            //    {
            //        tr = new TextRange(rtbx.Document.ContentStart, rtbx.Document.ContentEnd);
            //        tr.Text.Remove(0, tr.Text.IndexOf('\n'));
            //        this.lineCount--;
            //    }
            //}

            //if (this.AutoScroll)
            //{
            //    rtbx.ScrollToEnd();
            //}

        }
        private static readonly TypeConverter colorConverter = new ColorConverter();

        //internal static void ChangeSelectionToLink(RichTextBox textBox, string text, string hyperlink)
        //{
        //    int selectionStart = textBox.SelectionStart;

        //    //using \v tag to hide hyperlink part of the text, and \v0 to end hiding. See http://stackoverflow.com/a/14339531/376066
        //    //so in the control the link would consist only of "<text>", but in link clicked event we would get "<text>#<hyperlink>"
        //    textBox.SelectedRtf = @"{\rtf1\ansi " + text + @"\v #" + hyperlink + @"\v0}";

        //    textBox.Select(selectionStart, text.Length + 1 + hyperlink.Length); //now select both visible and invisible part
        //    SetSelectionStyle(textBox, CFM_LINK, CFE_LINK);                     //and turn into a link
        //}

        /// <summary>
        /// Stores a new message in internal queue, if it exists. Removes overflowing messages.
        /// </summary>
        /// <param name="logMessage">a message to store</param>
        /// <param name="rule">a corresponding coloring rule</param>
        /// <param name="logEvent">original LogEvent</param>
        private void StoreMessage(string logMessage, RichTextBoxRowColoringRule rule, LogEventInfo logEvent)
        {
            lock (messageQueueLock)
            {
                if (messageQueue == null)
                {
                    return;
                }
                if (MaxLines > 0)
                {
                    while (messageQueue.Count >= MaxLines)
                    {
                        messageQueue.Dequeue();
                    }
                }
                messageQueue.Enqueue(new MessageInfo(logMessage, rule, logEvent));
            }
        }


        /// <summary>
        /// Finds control of specified type embended on searchControl.
        /// </summary>
        /// <typeparam name="TControl">The type of the control.</typeparam>
        /// <param name="name">Name of the control.</param>
        /// <param name="searchControl">Control in which we're searching for control.</param>
        /// <returns>
        /// A value of null if no control has been found.
        /// </returns>
        //private TControl FindControl<TControl>(string name, Control searchControl)
        //     where TControl : Control
        // {
        //     if (searchControl.Name == name)
        //     {
        //         TControl foundControl = searchControl as TControl;
        //         if (foundControl != null)
        //         {
        //             return foundControl;
        //         }
        //     }

        //     foreach (Control childControl in searchControl.Controls)
        //     {
        //         TControl foundControl = FindControl<TControl>(name, childControl);

        //         if (foundControl != null)
        //         {
        //             return foundControl;
        //         }
        //     }

        //     return null;
        // }


        internal static Window CreateForm(string name, int width, int height, bool show, bool showMinimized, bool toolWindow)
        {
            var f = new Window
            {
                Name = name,
                Title = "NLog",
                //Icon = GetNLogIcon()
            };

#if !Smartphone
            if (toolWindow)
            {
                f.WindowStyle = WindowStyle.ToolWindow;
            }
#endif
            if (width > 0)
            {
                f.Width = width;
            }

            if (height > 0)
            {
                f.Height = height;
            }

            if (show)
            {
                if (showMinimized)
                {
                    f.WindowState = WindowState.Minimized;
                    f.Show();
                }
                else
                {
                    f.Show();
                }
            }

            return f;
        }
        //private static Icon GetNLogIcon()
        //{
        //    using (var stream = typeof(RichTextBoxTarget).Assembly.GetManifestResourceStream("NLog.Windows.Forms.Resources.NLog.ico"))
        //    {
        //        return new Icon(stream);
        //    }
        //}

        private class MessageInfo
        {
            internal string Message { get; private set; }
            internal RichTextBoxRowColoringRule Rule { get; private set; }
            internal LogEventInfo LogEvent { get; private set; }
            internal MessageInfo(string message, RichTextBoxRowColoringRule rule, LogEventInfo logEvent)
            {
                this.Message = message;
                this.Rule = rule;
                this.LogEvent = logEvent;
            }
        }



    }
}