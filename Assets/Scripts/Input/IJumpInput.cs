public interface IJumpInput
{
    bool JumpPressedThisFrame { get; }
    bool JumpHeld { get; }
    bool JumpReleasedThisFrame { get; }
}
