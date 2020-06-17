using NLog;
using NLog.Conditions;
using NLog.Config;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace NLog.WPF
{
    /// <summary>
    /// The row-coloring condition.
    /// 不同日志等级的显示颜色——高亮显示
    /// </summary>
    [NLogConfigurationItem]
    public class RichTextBoxRowColoringRule
    {
        /// <summary>
        /// Gets the default highlighting rule. Doesn't change the color.
        /// </summary>
        public static RichTextBoxRowColoringRule Default { get; private set; }

        /// <summary>
        /// Gets or sets the condition that must be met in order to set the specified font color.
        /// </summary>
        [RequiredParameter]
        public ConditionExpression Condition { get; set; }

        /// <summary>
        /// Gets or sets the font color.
        /// </summary>
        /// 
        /// <remarks>
        /// Names are identical with KnownColor enum extended with Empty value which means that background color won't be changed.
        /// </remarks>
        [DefaultValue("Empty")]
        public string FontColor { get; set; }

        /// <summary>
        /// Gets or sets the background color.
        /// </summary>
        /// 
        /// <remarks>
        /// Names are identical with KnownColor enum extended with Empty value which means that background color won't be changed.
        /// </remarks>
        [DefaultValue("Empty")]
        public string BackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the font style of matched text.
        /// 字体样式——倾斜
        /// </summary>
        /// 
        /// <remarks>
        /// Possible values are the same as in <c>FontStyle</c> enum in <c>System.Drawing</c>
        /// </remarks>
        /// <docgen category="Formatting Options" order="10"/>
        public FontStyle Style { get; set; }

        /// <summary>
        /// 字体粗细
        /// </summary>
        public FontWeight Weight { get; set; }
        //TextDecorations
        //Run
        //Inline
        //TextRange
        //Block
        //FlowDocument

        /// <summary>
        /// Initializes static members of the RichTextBoxRowColoringRule class.
        /// </summary>
        static RichTextBoxRowColoringRule()
        {
            RichTextBoxRowColoringRule.Default = new RichTextBoxRowColoringRule();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NLog.Targets.RichTextBoxRowColoringRule"/> class.
        /// </summary>
        public RichTextBoxRowColoringRule() : this((string)null, "Empty", "Empty", FontStyles.Normal, FontWeights.Normal)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NLog.Targets.RichTextBoxRowColoringRule"/> class.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <param name="fontColor">Color of the foreground text.</param>
        /// <param name="backColor">Color of the background text.</param>
        /// <param name="fontStyle">The font style.</param>
        public RichTextBoxRowColoringRule(string condition, string fontColor, string backColor, FontStyle fontStyle, FontWeight fontWeight)
        {
            this.Condition = (ConditionExpression)condition;
            this.FontColor = fontColor;
            this.BackgroundColor = backColor;
            this.Style = fontStyle;
            this.Weight = fontWeight;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NLog.Targets.RichTextBoxRowColoringRule"/> class.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <param name="fontColor">Color of the text.</param>
        /// <param name="backColor">Color of the background.</param>
        public RichTextBoxRowColoringRule(string condition, string fontColor, string backColor)
        {
            this.Condition = (ConditionExpression)condition;
            this.FontColor = fontColor;
            this.BackgroundColor = backColor;
            this.Style = FontStyles.Normal;
            this.Weight = FontWeights.Normal;
        }

        /// <summary>
        /// Checks whether the specified log event matches the condition (if any).
        /// </summary>
        /// <param name="logEvent">Log event.</param>
        /// <returns>
        /// A value of <see langword="true"/> if the condition is not defined or
        ///             if it matches, <see langword="false"/> otherwise.
        /// </returns>
        public bool CheckCondition(LogEventInfo logEvent)
        {
            return true.Equals(this.Condition.Evaluate(logEvent));
        }
    }
}
