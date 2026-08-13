using System;

namespace LowDefMustard.Utils
{
    public interface IEnumKeyedCollection
    {
        Type GetEnumType();
        string GetListName();
    }
}
