namespace Boolean;

public static class Selection
{
    public static BooleanPatchSet Run(
        BooleanOperationType operation,
        PatchClassification classification,
        IntersectionGraph graph)
    {
        var selected = PatchSelector.Select(operation, classification, graph);
        SelectionDiagnostics.DumpIfEnabled(operation, classification, graph, selected);
        return selected;
    }
}
