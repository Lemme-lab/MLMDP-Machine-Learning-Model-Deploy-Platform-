namespace ControlPlaneAPI;

public class Pod
{
    public String podServiceIp { get; set; }
    public String podServiceIpPort { get; set; }
    public String podName { get; set; }
    public String status { get; set; }
    public String specContainers { get; set; }
    public String statusMessage { get; set; }

    public Pod(string podServiceIp, string podName, string status, string specContainers, string statusMessage, string podServiceIpPort)
    {
        this.podServiceIp = podServiceIp;
        this.podName = podName;
        this.status = status;
        this.specContainers = specContainers;
        this.statusMessage = statusMessage;
        this.podServiceIpPort = podServiceIpPort;
    }
    
    // Override ToString method
    public override string ToString()
    {
        return $"Pod Name: {podName}, " +
               $"Service IP: {podServiceIp}, " +
               $"Status: {status}, " +
               $"Containers: {specContainers}, " +
               $"Status Message: {statusMessage}" +
               $"Service Port: {podServiceIpPort}";
    }
}