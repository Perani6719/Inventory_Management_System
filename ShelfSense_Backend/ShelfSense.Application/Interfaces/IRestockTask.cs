//using System;

//using System.Collections.Generic;

//using System.Linq;

//using System.Text;

//using System.Threading.Tasks;

//namespace ShelfSense.Application.Interfaces

//{

//    public interface IRestockTaskRepository

//    {

//        Task<List<RestockTask>> GetAllAsync();

//        Task<RestockTask?> GetByIdAsync(long taskId);

//        Task<List<RestockTask>> GetByStaffIdAsync(long staffId);

//        Task<List<RestockTask>> GetDelayedTasksAsync();

//        Task AssignTasksFromDeliveredStockAsync();

//        Task<string?> OrganizeDeliveredProductAsync(long taskId, long staffId);




//        Task<string?> CheckStatusByIdAsync(long taskId);


//    }

//}

using System;

using System.Collections.Generic;

using System.Linq;

using System.Text;

using System.Threading.Tasks;

namespace ShelfSense.Application.Interfaces

{

    public interface IRestockTaskRepository

    {

        Task<List<RestockTask>> GetAllAsync();

        Task<RestockTask?> GetByIdAsync(long taskId);

        Task<List<RestockTask>> GetByStaffIdAsync(long staffId);

        Task<List<RestockTask>> GetDelayedTasksAsync();

        Task<string?> AssignTasksFromDeliveredStockAsync();

        Task<string?> OrganizeDeliveredProductAsync(long taskId, long staffId);



        Task<string?> CheckStatusByIdAsync(long taskId);


    }

}