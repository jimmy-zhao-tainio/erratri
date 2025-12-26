namespace Boolean;

public static class Selection
{
    public static BooleanPatchSet Run(BooleanOperationType operation, PatchClassification classification)
    {
        return PatchSelector.Select(operation, classification);
    }
}
