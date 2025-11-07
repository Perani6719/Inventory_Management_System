using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Application.Interfaces
{
    public interface IWarehouseRequestService
    {
        Task<bool> RaiseManualRequest(string storeAlertId, string urgencyLevel);
    }
}
