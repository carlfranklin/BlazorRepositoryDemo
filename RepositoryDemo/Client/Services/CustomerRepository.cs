public class CustomerRepoistory : APIRepository<Customer>
{
    HttpClient http;
    
    // swap out the controller name
    static string controllerName = "inmemorycustomers";
    //static string controllerName = "efcustomers";

    public CustomerRepoistory(HttpClient _http)
       : base(_http, controllerName, "Id")
    {
        http = _http;
    }
}