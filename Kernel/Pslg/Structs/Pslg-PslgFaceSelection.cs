using System;
using System.Collections.Generic;

namespace Kernel;

internal readonly struct PslgFaceSelection
{
    public IReadOnlyList<PslgFace> InteriorFaces { get; }

    public PslgFaceSelection(IReadOnlyList<PslgFace> interiorFaces)
    {
        InteriorFaces = interiorFaces ?? throw new ArgumentNullException(nameof(interiorFaces));
    }
}
