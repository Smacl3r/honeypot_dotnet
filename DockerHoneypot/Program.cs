// See https://aka.ms/new-console-template for more information

using Docker.DotNet;
using Docker.DotNet.Models;
using DockerHoneypot.Helpers;
using System.ComponentModel;
using System.Runtime.InteropServices;

var progress = new Progress<JSONMessage>();
var captureTarStream = TarForDockerFileDir.CreateTarballForDockerfileDirectory("../../../Images/capture");
var captureImageBuildParameters = new ImageBuildParameters() { Tags = new List<string> { "capture" }, Dockerfile = "../../../Images/capture/Dockerfile" };

var dockerClient = new DockerClientConfiguration().CreateClient();
//RESET
var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
foreach (var container in containers)
{
    if(container.Status == "Created")
    {
        Console.WriteLine($"Removing container: {container.Image}");
        await dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters());
    }
    else if(container.State == "running" || container.State == "exited")
    {
        Console.WriteLine($"Stopping container: {container.Image}");
        await dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters() { WaitBeforeKillSeconds = 10 });
        Console.WriteLine($"Removing container: {container.Image}");
        await dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters());
    }
}


//var captureStream = File.OpenRead(pathToCaptureTar);
Console.WriteLine($"Building image...");
await dockerClient.Images.BuildImageFromDockerfileAsync(captureImageBuildParameters, captureTarStream, null, null, progress);

using var saveCaptureImageStream = await dockerClient.Images.SaveImageAsync("capture");
Console.WriteLine($"Creating container...");
var createNetworkContainerResponse = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters() { Name = "whaler_capture", Image = "capture", Entrypoint = new List<string>() { "/bin/sh", "/app/wrapper.sh" } });
Console.WriteLine($"Starting container...");
await dockerClient.Containers.StartContainerAsync(createNetworkContainerResponse.ID, new ContainerStartParameters());
Console.WriteLine($"Container started.");

//var reportingImageBuildParameters = new ImageBuildParameters() { Tags = new List<string> { "reporting" } };
////dockerClient.Images.BuildImageFromDockerfileAsync();
//using var reportingTarball = TarForDockerFileDir.CreateTarballForDockerfileDirectory("../../../Images/reporting");
//using var reportingResponseStream = await dockerClient.Images.BuildImageFromDockerfileAsync(reportingTarball, reportingImageBuildParameters);

//using var saveReportingImageStream = await dockerClient.Images.SaveImageAsync("reporting");