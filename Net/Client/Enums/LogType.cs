namespace Client.Enums
{
    public enum LogType
    {
        /// <summary>
        /// tracing information and debugging minutiae; generally only switched on in unusual situations
        /// </summary>
        Verbose = 0,
        /// <summary>
        /// internal control flow and diagnostic state dumps to facilitate pinpointing of recognised problems
        /// </summary>
        Debug = 1,
        /// <summary>
        /// events of interest or that have relevance to outside observers; the default enabled minimum logging level
        /// </summary>
        Information = 2,
        /// <summary>
        /// indicators of possible issues or service/functionality degradation
        /// </summary>
        Warning = 3,
        /// <summary>
        /// indicating a failure within the application or connected system
        /// </summary>
        Error = 4,
        /// <summary>
        /// critical errors causing complete failure of the application
        /// </summary>
        Fatal = 5
    }
}
