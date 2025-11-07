using ShelfSense.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Application.Interfaces
{
    public interface IDeliveryStatusLog
    {
        Task<List<DeliveryStatusLog>> GetAllAsync();
        Task AddAsync(DeliveryStatusLog log);
    }
}
