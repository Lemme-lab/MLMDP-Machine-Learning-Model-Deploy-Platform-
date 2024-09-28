using k8s;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Define the namespace name
var namespaceName = "model-deployments"; // New namespace name

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddSingleton<IKubernetes>(sp =>
{
    // Check if we're running inside Kubernetes
    if (File.Exists("/var/run/secrets/kubernetes.io/serviceaccount/token"))
    {
        // Use the in-cluster configuration
        return new Kubernetes(KubernetesClientConfiguration.InClusterConfig());
    }
    else
    {
        // Running locally, use kubeconfig
        var kubeConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".kube/config");
        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeConfigPath);
        return new Kubernetes(config);
    }
});

// Pass the namespace to the controller via dependency injection
builder.Services.AddSingleton(namespaceName);

var app = builder.Build();

// Enable CORS globally
app.UseCors("AllowAll");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.MapControllers();
app.Run();
