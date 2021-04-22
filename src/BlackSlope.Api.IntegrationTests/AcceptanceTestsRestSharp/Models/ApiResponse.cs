namespace AcceptanceTestsRestSharp.Models
{
    public class ApiResponse<T>
    {


        public ApiResponse()
        {
        }

        public ApiResponse(T data)
        {
            Data = data;
        }

        public T Data { get; set; }

    }

    public class ApiResponse
    {
        public object Data { get; set; }

    }



}




