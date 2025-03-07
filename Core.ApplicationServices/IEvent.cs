using System.Collections.Generic;

namespace Core.ApplicationServices;

public interface IEvent
{
    Dictionary<string, object> ToKeyValuePairs();
}