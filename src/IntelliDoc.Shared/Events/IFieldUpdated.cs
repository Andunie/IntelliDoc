using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelliDoc.Shared.Events;

public interface IFieldUpdated
{
    Guid DocumentId { get; }
    string FieldName { get; }
    string NewValue { get; }
    string UpdatedBy { get; }
    DateTime UpdatedAt { get; }
}
