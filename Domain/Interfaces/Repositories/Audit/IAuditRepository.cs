using Domain.Models.Audit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces.Repositories.Audit;

public interface IAuditRepository
{
    Task SaveEventLog(SaveEventInModel evetParam);

}
