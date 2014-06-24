
namespace AdRotator.Model
{
    // Needs to be accesible from project
    public enum SlideDirection
    {
        /// <summary>
        /// The ad will slide in/out from/to the top
        /// </summary>
        Top,
        /// <summary>
        /// The ad will slide in/out from/to the bottom
        /// </summary>
        Bottom,
        /// <summary>
        /// The ad will slide in/out from/to the left
        /// </summary> 
        Left,
        /// <summary>
        /// The ad will slide in/out from/to the right
        /// </summary>
        Right,
        /// <summary>
        /// The ad will not slide, it will just act as static
        /// </summary>
        None
    }

    /// <summary>
    /// Enum describes the Ad transition state.
    /// </summary>
    public enum AdTransitionState
    {
        TransitionOn,
        Active,
        TransitionOff,
        Hidden,
    }
}
