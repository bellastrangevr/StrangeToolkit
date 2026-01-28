using UnityEngine;

namespace StrangeToolkit
{
    /// <summary>
    /// Centralized logging utility for Strange Toolkit with colored console output.
    /// </summary>
    public static class StrangeToolkitLogger
    {
        // Prefix and color constants
        private const string PREFIX = "<b><color=#9B59B6>[Strange Toolkit]</color></b>";
        private const string COLOR_SUCCESS = "#2ECC71";  // Green
        private const string COLOR_WARNING = "#F39C12";  // Orange
        private const string COLOR_ERROR = "#E74C3C";    // Red
        private const string COLOR_INFO = "#3498DB";     // Blue
        private const string COLOR_HIGHLIGHT = "#9B59B6"; // Purple

        /// <summary>
        /// Log an info message.
        /// </summary>
        public static void Log(string message)
        {
            Debug.Log($"{PREFIX} {message}");
        }

        /// <summary>
        /// Log a success message (green).
        /// </summary>
        public static void LogSuccess(string message)
        {
            Debug.Log($"{PREFIX} <color={COLOR_SUCCESS}>{message}</color>");
        }

        /// <summary>
        /// Log a warning message (orange).
        /// </summary>
        public static void LogWarning(string message)
        {
            Debug.LogWarning($"{PREFIX} <color={COLOR_WARNING}>{message}</color>");
        }

        /// <summary>
        /// Log an error message (red).
        /// </summary>
        public static void LogError(string message)
        {
            Debug.LogError($"{PREFIX} <color={COLOR_ERROR}>{message}</color>");
        }

        /// <summary>
        /// Log a message with custom formatting.
        /// </summary>
        /// <param name="message">The main message</param>
        /// <param name="details">Additional details (shown on new line, indented)</param>
        public static void LogWithDetails(string message, string details)
        {
            Debug.Log($"{PREFIX} {message}\n    {details}");
        }

        /// <summary>
        /// Log a success message with details.
        /// </summary>
        public static void LogSuccessWithDetails(string message, string details)
        {
            Debug.Log($"{PREFIX} <color={COLOR_SUCCESS}>{message}</color>\n    <color={COLOR_INFO}>{details}</color>");
        }

        /// <summary>
        /// Log an action taken on an object.
        /// </summary>
        /// <param name="action">The action (e.g., "Removed", "Fixed", "Updated")</param>
        /// <param name="target">What was affected (e.g., "Avatar Descriptor")</param>
        /// <param name="objectName">Name of the GameObject</param>
        /// <param name="objectPath">Optional hierarchy path</param>
        public static void LogAction(string action, string target, string objectName, string objectPath = null)
        {
            string msg = $"{PREFIX} <color={COLOR_ERROR}>{action}</color> {target} from <color={COLOR_INFO}>\"{objectName}\"</color>";
            if (!string.IsNullOrEmpty(objectPath))
            {
                msg += $"\n    Path: {objectPath}";
            }
            Debug.Log(msg);
        }

        /// <summary>
        /// Log detection of issues.
        /// </summary>
        /// <param name="count">Number of issues found</param>
        /// <param name="issueType">Type of issue (e.g., "Avatar Descriptor(s)")</param>
        public static void LogDetection(int count, string issueType)
        {
            Debug.Log($"{PREFIX} <color={COLOR_WARNING}>Detected {count} {issueType} in scene</color>");
        }

        /// <summary>
        /// Log a summary of actions taken.
        /// </summary>
        /// <param name="action">What was done (e.g., "removed", "fixed")</param>
        /// <param name="count">Number of items affected</param>
        /// <param name="itemType">Type of items (e.g., "Avatar Descriptor(s)")</param>
        /// <param name="tip">Optional helpful tip</param>
        public static void LogSummary(string action, int count, string itemType, string tip = null)
        {
            string msg = $"{PREFIX} <color={COLOR_SUCCESS}>Successfully {action} {count} {itemType}</color>";
            if (!string.IsNullOrEmpty(tip))
            {
                msg += $"\n    <color={COLOR_INFO}>Tip: {tip}</color>";
            }
            Debug.Log(msg);
        }

        /// <summary>
        /// Highlight text with the info color (blue).
        /// </summary>
        public static string Highlight(string text)
        {
            return $"<color={COLOR_INFO}>{text}</color>";
        }

        /// <summary>
        /// Format text as success (green).
        /// </summary>
        public static string Success(string text)
        {
            return $"<color={COLOR_SUCCESS}>{text}</color>";
        }

        /// <summary>
        /// Format text as warning (orange).
        /// </summary>
        public static string Warning(string text)
        {
            return $"<color={COLOR_WARNING}>{text}</color>";
        }

        /// <summary>
        /// Format text as error (red).
        /// </summary>
        public static string Error(string text)
        {
            return $"<color={COLOR_ERROR}>{text}</color>";
        }
    }
}
