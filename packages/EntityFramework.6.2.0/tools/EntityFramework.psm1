# Copyright (c) Microsoft Corporation.  All rights reserved.

$InitialDatabase = '0'

$knownExceptions = @(
    'System.Data.Entity.Migrations.Infrastructure.MigrationsException',
    'System.Data.Entity.Migrations.Infrastructure.AutomaticMigrationsDisabledException',
    'System.Data.Entity.Migrations.Infrastructure.AutomaticDataLossException',
    'System.Data.Entity.Migrations.Infrastructure.MigrationsPendingException',
    'System.Data.Entity.Migrations.ProjectTypeNotSupportedException'
)

<#
.SYNOPSIS
    Adds or updates an Entity Framework provider entry in the project config
    file.

.DESCRIPTION
    Adds an entry into the 'entityFramework' section of the project config
    file for the specified provider invariant name and provider type. If an
    entry for the given invariant name already exists, then that entry is
    updated with the given type name, unless the given type name already
    matches, in which case no action is taken. The 'entityFramework'
    section is added if it does not exist. The config file is automatically
    saved if and only if a change was made.
    
    This command is typically used only by Entity Framework provider NuGet
    packages and is run from the 'install.ps1' script.

.PARAMETER Project
    The Visual Studio project to update. When running in the NuGet install.ps1
    script the '$project' variable provided as part of that script should be
    used.

.PARAMETER InvariantName
    The provider invariant name that uniquely identifies this provider. For
    example, the Microsoft SQL Server provider is registered with the invariant
    name 'System.Data.SqlClient'.

.PARAMETER TypeName
    The assembly-qualified type name of the provider-specific type that
    inherits from 'System.Data.Entity.Core.Common.DbProviderServices'. For
    example, for the Microsoft SQL Server provider, this type is 
    'System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer'.
#>
function Add-EFProvider
{
    param (
        [parameter(Position = 0,
            Mandatory = $true)]
        $Project,
        [parameter(Position = 1,
            Mandatory = $true)]
        [string] $InvariantName,
        [parameter(Position = 2,
            Mandatory = $true)]
        [string] $TypeName
    )

	if (!(Check-Project $project))
	{
	    return
	}

    $runner = New-EFConfigRunner $Project

    try
    {
        Invoke-RunnerCommand $runner System.Data.Entity.ConnectionFactoryConfig.AddProviderCommand @( $InvariantName, $TypeName )
        $error = Get-RunnerError $runner

        if ($error)
        {
            if ($knownExceptions -notcontains $error.TypeName)
            {
                Write-Host $error.StackTrace
            }
            else
            {
                Write-Verbose $error.StackTrace
            }

            throw $error.Message
        }
    }
    finally
    {				
        Remove-Runner $runner
    }
}

<#
.SYNOPSIS
    Adds or updates an Entity Framework default connection factory in the
    project config file.

.DESCRIPTION
    Adds an entry into the 'entityFramework' section of the project config
    file for the connection factory that Entity Framework will use by default
    when creating new connections by convention. Any existing entry will be
    overridden if it does not match. The 'entityFramework' section is added if
    it does not exist. The config file is automatically saved if and only if
    a change was made.
    
    This command is typically used only by Entity Framework provider NuGet
    packages and is run from the 'install.ps1' script.

.PARAMETER Project
    The Visual Studio project to update. When running in the NuGet install.ps1
    script the '$project' variable provided as part of that script should be
    used.

.PARAMETER TypeName
    The assembly-qualified type name of the connection factory type that
    implements the 'System.Data.Entity.Infrastructure.IDbConnectionFactory'
    interface.  For example, for the Microsoft SQL Server Express provider
    connection factory, this type is
    'System.Data.Entity.Infrastructure.SqlConnectionFactory, EntityFramework'.

.PARAMETER ConstructorArguments
    An optional array of strings that will be passed as arguments to the
    connection factory type constructor.
#>
function Add-EFDefaultConnectionFactory
{
    param (
        [parameter(Position = 0,
            Mandatory = $true)]
        $Project,
        [parameter(Position = 1,
            Mandatory = $true)]
        [string] $TypeName,
        [string[]] $ConstructorArguments
    )

	if (!(Check-Project $project))
	{
	    return
	}

    $runner = New-EFConfigRunner $Project

    try
    {
        Invoke-RunnerCommand $runner System.Data.Entity.ConnectionFactoryConfig.AddDefaultConnectionFactoryCommand @( $TypeName, $ConstructorArguments )
        $error = Get-RunnerError $runner

        if ($error)
        {
            if ($knownExceptions -notcontains $error.TypeName)
            {
                Write-Host $error.StackTrace
            }
            else
            {
                Write-Verbose $error.StackTrace
            }

            throw $error.Message
        }
    }
    finally
    {				
        Remove-Runner $runner
    }
}

<#
.SYNOPSIS
    Initializes the Entity Framework section in the project config file
    and sets defaults.

.DESCRIPTION
    Creates the 'entityFramework' section of the project config file and sets
    the default connection factory to use SQL Express if it is running on the
    machine, or LocalDb otherwise. Note that installing a different provider
    may change the default connection factory.  The config file is
    automatically saved if and only if a change was made.

    In addition, any reference to 'System.Data.Entity.dll' in the project is
    removed.
    
    This command is typically used only by Entity Framework provider NuGet
    packages and is run from the 'install.ps1' script.

.PARAMETER Project
    The Visual Studio project to update. When running in the NuGet install.ps1
    script the '$project' variable provided as part of that script should be
    used.
#>
function Initialize-EFConfiguration
{
    param (
        [parameter(Position = 0,
            Mandatory = $true)]
        $Project
    )

	if (!(Check-Project $project))
	{
	    return
	}

    $runner = New-EFConfigRunner $Project

    try
    {
        Invoke-RunnerCommand $runner System.Data.Entity.ConnectionFactoryConfig.InitializeEntityFrameworkCommand
        $error = Get-RunnerError $runner

        if ($error)
        {
            if ($knownExceptions -notcontains $error.TypeName)
            {
                Write-Host $error.StackTrace
            }
            else
            {
                Write-Verbose $error.StackTrace
            }

            throw $error.Message
        }
    }
    finally
    {				
        Remove-Runner $runner
    }
}

<#
.SYNOPSIS
    Enables Code First Migrations in a project.

.DESCRIPTION
    Enables Migrations by scaffolding a migrations configuration class in the project. If the
    target database was created by an initializer, an initial migration will be created (unless
    automatic migrations are enabled via the EnableAutomaticMigrations parameter).

.PARAMETER ContextTypeName
    Specifies the context to use. If omitted, migrations will attempt to locate a
    single context type in the target project.

.PARAMETER EnableAutomaticMigrations
    Specifies whether automatic migrations will be enabled in the scaffolded migrations configuration.
    If omitted, automatic migrations will be disabled.

.PARAMETER MigrationsDirectory
    Specifies the name of the directory that will contain migrations code files.
    If omitted, the directory will be named "Migrations". 

.PARAMETER ProjectName
    Specifies the project that the scaffolded migrations configuration class will
    be added to. If omitted, the default project selected in package manager
    console is used.

.PARAMETER StartUpProjectName
    Specifies the configuration file to use for named connection strings. If
    omitted, the specified project's configuration file is used.

.PARAMETER ContextProjectName
    Specifies the project which contains the DbContext class to use. If omitted,
    the context is assumed to be in the same project used for migrations.

.PARAMETER ConnectionStringName
    Specifies the name of a connection string to use from the application's
    configuration file.

.PARAMETER ConnectionString
    Specifies the the connection string to use. If omitted, the context's
    default connection will be used.

.PARAMETER ConnectionProviderName
    Specifies the provider invariant name of the connection string.

.PARAMETER Force
    Specifies that the migrations configuration be overwritten when running more
    than once for a given project.
	
.PARAMETER ContextAssemblyName
    Specifies the name of the assembly which contains the DbContext class to use. Use this
    parameter instead of ContextProjectName when the context is contained in a referenced
    assembly rather than in a project of the solution.

.PARAMETER AppDomainBaseDirectory
    Specifies the directory to use for the app-domain that is used for running Migrations
    code such that the app-domain is able to find all required assemblies. This is an
    advanced option that should only be needed if the solution contains	several projects 
    such that the assemblies needed for the context and configuration are not all
    referenced from either the project containing the context or the project containing
    the migrations.

.EXAMPLE 
	Enable-Migrations
	# Scaffold a migrations configuration in a project with only one context
	
.EXAMPLE 
	Enable-Migrations -Auto
	# Scaffold a migrations configuration with automatic migrations enabled for a project
	# with only one context
	
.EXAMPLE 
	Enable-Migrations -ContextTypeName MyContext -MigrationsDirectory DirectoryName
	# Scaffold a migrations configuration for a project with multiple contexts
	# This scaffolds a migrations configuration for MyContext and will put the configuration
	# and subsequent configurations in a new directory called "DirectoryName"

#>
function Enable-Migrations
{
    [CmdletBinding(DefaultParameterSetName = 'ConnectionStringName')] 
    param (
        [string] $ContextTypeName,
        [alias('Auto')]
        [switch] $EnableAutomaticMigrations,
        [string] $MigrationsDirectory,
        [string] $ProjectName,
        [string] $StartUpProjectName,
        [string] $ContextProjectName,
        [parameter(ParameterSetName = 'ConnectionStringName')]
        [string] $ConnectionStringName,
        [parameter(ParameterSetName = 'ConnectionStringAndProviderName',
            Mandatory = $true)]
        [string] $ConnectionString,
        [parameter(ParameterSetName = 'ConnectionStringAndProviderName',
            Mandatory = $true)]
        [string] $ConnectionProviderName,
        [switch] $Force,
        [string] $ContextAssemblyName,
		[string] $AppDomainBaseDirectory
    )

    $runner = New-MigrationsRunner $ProjectName $StartUpProjectName $ContextProjectName $null $ConnectionStringName $ConnectionString $ConnectionProviderName $ContextAssemblyName $AppDomainBaseDirectory

    try
    {
        Invoke-RunnerCommand $runner System.Data.Entity.Migrations.EnableMigrationsCommand @( $EnableAutomaticMigrations.IsPresent, $Force.IsPresent ) @{ 'ContextTypeName' = $ContextTypeName; 'MigrationsDirectory' = $MigrationsDirectory }        		
        $error = Get-RunnerError $runner					

        if ($error)
        {
            if ($knownExceptions -notcontains $error.TypeName)
            {
                Write-Host $error.StackTrace
            }
            else
            {
                Write-Verbose $error.StackTrace
            }

            throw $error.Message
        }

        $(Get-VSComponentModel).GetService([NuGetConsole.IPowerConsoleWindow]).Show()	        
    }
    finally
    {				
        Remove-Runner $runner		
    }
}

<#
.SYNOPSIS
    Scaffolds a migration script for any pending model changes.

.DESCRIPTION
    Scaffolds a new migration script and adds it to the project.

.PARAMETER Name
    Specifies the name of the custom script.

.PARAMETER Force
    Specifies that the migration user code be overwritten when re-scaffolding an
    existing migration.

.PARAMETER ProjectName
    Specifies the project that contains the migration configuration type to be
    used. If omitted, the default project selected in package manager console
    is used.

.PARAMETER StartUpProjectName
    Specifies the configuration file to use for named connection strings. If
    omitted, the specified project's configuration file is used.

.PARAMETER ConfigurationTypeName
    Specifies the migrations configuration to use. If omitted, migrations will
    attempt to locate a single migrations configuration type in the target
    project.

.PARAMETER ConnectionStringName
    Specifies the name of a connection string to use from the application's
    configuration file.

.PARAMETER ConnectionString
    Specifies the the connection string to use. If omitted, the context's
    default connection will be used.

.PARAMETER ConnectionProviderName
    Specifies the provider invariant name of the connection string.

.PARAMETER IgnoreChanges
    Scaffolds an empty migration ignoring any pending changes detected in the current model.
    This can be used to create an initial, empty migration to enable Migrations for an existing
    database. N.B. Doing this assumes that the target database schema is compatible with the
    current model.

.PARAMETER AppDomainBaseDirectory
    Specifies the directory to use for the app-domain that is used for running Migrations
    code such that the app-domain is able to find all required assemblies. This is an
    advanced option that should only be needed if the solution contains	several projects 
    such that the assemblies needed for the context and configuration are not all
    referenced from either the project containing the context or the project containing
    the migrations.
	
.EXAMPLE
	Add-Migration First
	# Scaffold a new migration named "First"
	
.EXAMPLE
	Add-Migration First -IgnoreChanges
	# Scaffold an empty migration ignoring any pending changes detected in the current model.
	# This can be used to create an initial, empty migration to enable Migrations for an existing
	# database. N.B. Doing this assumes that the target database schema is compatible with the
	# current model.

#>
function Add-Migration
{
    [CmdletBinding(DefaultParameterSetName = 'ConnectionStringName')]
    param (
        [parameter(Position = 0,
            Mandatory = $true)]
        [string] $Name,
        [switch] $Force,
        [string] $ProjectName,
        [string] $StartUpProjectName,
        [string] $ConfigurationTypeName,
        [parameter(ParameterSetName = 'ConnectionStringName')]
        [string] $ConnectionStringName,
        [parameter(ParameterSetName = 'ConnectionStringAndProviderName',
            Mandatory = $true)]
        [string] $ConnectionString,
        [parameter(ParameterSetName = 'ConnectionStringAndProviderName',
            Mandatory = $true)]
        [string] $ConnectionProviderName,
        [switch] $IgnoreChanges,
		[string] $AppDomainBaseDirectory)

    Hint-Downgrade $MyInvocation.MyCommand
    $runner = New-MigrationsRunner $ProjectName $StartUpProjectName $null $ConfigurationTypeName $ConnectionStringName $ConnectionString $ConnectionProviderName $null $AppDomainBaseDirectory

    try
    {
        Invoke-RunnerCommand $runner System.Data.Entity.Migrations.AddMigrationCommand @( $Name, $Force.IsPresent, $IgnoreChanges.IsPresent )
        $error = Get-RunnerError $runner       		

        if ($error)
        {
            if ($knownExceptions -notcontains $error.TypeName)
            {
                Write-Host $error.StackTrace
            }
            else
            {
                Write-Verbose $error.StackTrace
            }

            throw $error.Message
        }		
        $(Get-VSComponentModel).GetService([NuGetConsole.IPowerConsoleWindow]).Show()	        
    }
    finally
    {			
        Remove-Runner $runner		
    }	
}

<#
.SYNOPSIS
    Applies any pending migrations to the database.

.DESCRIPTION
    Updates the database to the current model by applying pending migrations.

.PARAMETER SourceMigration
    Only valid with -Script. Specifies the name of a particular migration to use
    as the update's starting point. If omitted, the last applied migration in
    the database will be used.

.PARAMETER TargetMigration
    Specifies the name of a particular migration to update the database to. If
    omitted, the current model will be used.

.PARAMETER Script
    Generate a SQL script rather than executing the pending changes directly.

.PARAMETER Force
    Specifies that data loss is acceptable during automatic migration of the
    database.

.PARAMETER ProjectName
    Specifies the project that contains the migration configuration type to be
    used. If omitted, the default project selected in package manager console
    is used.

.PARAMETER StartUpProjectName
    Specifies the configuration file to use for named connection strings. If
    omitted, the specified project's configuration file is used.

.PARAMETER ConfigurationTypeName
    Specifies the migrations configuration to use. If omitted, migrations will
    attempt to locate a single migrations configuration type in the target
    project.

.PARAMETER ConnectionStringName
    Specifies the name of a connection string to use from the application's
    configuration file.

.PARAMETER ConnectionString
    Specifies the the connection string to use. If omitted, the context's
    default connection will be used.

.PARAMETER ConnectionProviderName
    Specifies the provider invariant name of the connection string.
	
.PARAMETER AppDomainBaseDirectory
    Specifies the directory to use for the app-domain that is used for running Migrations
    code such that the app-domain is able to find all required assemblies. This is an
    advanced option that should only be needed if the solution contains	several projects 
    such that the assemblies needed for the context and configuration are not all
    referenced from either the project containing the context or the project containing
    the migrations.

.EXAMPLE
	Update-Database
	# Update the database to the latest migration
	
.EXAMPLE
	Update-Database -TargetMigration Second
	# Update database to a migration named "Second"
	# This will apply migrations if the target hasn't been applied or roll back migrations
	# if it has
	
.EXAMPLE
	Update-Database -Script
	# Generate a script to update the database from it's current state  to the latest migration
	
.EXAMPLE
	Update-Database -Script -SourceMigration Second -TargetMigration First
	# Generate a script to migrate the database from a specified start migration
	# named "Second" to a specified target migration named "First"	
	
.EXAMPLE
	Update-Database -Script -SourceMigration $InitialDatabase
	# Generate a script that can upgrade a database currently at any version to the latest version. 
	# The generated script includes logic to check the __MigrationsHistory table and only apply changes 
	# that haven't been previously applied.	
	
.EXAMPLE 
	Update-Database -TargetMigration $InitialDatabase
	# Runs the Down method to roll-back any migrations that have been applied to the database
	
	
#>
function Update-Database
{
    [CmdletBinding(DefaultParameterSetName = 'ConnectionStringName')]
    param (
        [string] $SourceMigration,
        [string] $TargetMigration,
        [switch] $Script,
        [switch] $Force,
        [string] $ProjectName,
        [string] $StartUpProjectName,
        [string] $ConfigurationTypeName,
        [parameter(ParameterSetName = 'ConnectionStringName')]
        [string] $ConnectionStringName,
        [parameter(ParameterSetName = 'ConnectionStringAndProviderName',
            Mandatory = $true)]
        [string] $ConnectionString,
        [parameter(ParameterSetName = 'ConnectionStringAndProviderName',
            Mandatory = $true)]
        [string] $ConnectionProviderName,
		[string] $AppDomainBaseDirectory)

    Hint-Downgrade $MyInvocation.MyCommand
    $runner = New-MigrationsRunner $ProjectName $StartUpProjectName $null $ConfigurationTypeName $ConnectionStringName $ConnectionString $ConnectionProviderName $null $AppDomainBaseDirectory

    try
    {
        Invoke-RunnerCommand $runner System.Data.Entity.Migrations.UpdateDatabaseCommand @( $SourceMigration, $TargetMigration, $Script.IsPresent, $Force.IsPresent, $Verbose.IsPresent )
        $error = Get-RunnerError $runner
        
        if ($error)
        {
            if ($knownExceptions -notcontains $error.TypeName)
            {
                Write-Host $error.StackTrace
            }
            else
            {
                Write-Verbose $error.StackTrace
            }

            throw $error.Message
        }		
        $(Get-VSComponentModel).GetService([NuGetConsole.IPowerConsoleWindow]).Show()	        
    }
    finally
    {		     
        Remove-Runner $runner
    }
}

<#
.SYNOPSIS
    Displays the migrations that have been applied to the target database.

.DESCRIPTION
    Displays the migrations that have been applied to the target database.

.PARAMETER ProjectName
    Specifies the project that contains the migration configuration type to be
    used. If omitted, the default project selected in package manager console
    is used.

.PARAMETER StartUpProjectName
    Specifies the configuration file to use for named connection strings. If
    omitted, the specified project's configuration file is used.

.PARAMETER ConfigurationTypeName
    Specifies the migrations configuration to use. If omitted, migrations will
    attempt to locate a single migrations configuration type in the target
    project.

.PARAMETER ConnectionStringName
    Specifies the name of a connection string to use from the application's
    configuration file.

.PARAMETER ConnectionString
    Specifies the the connection string to use. If omitted, the context's
    default connection will be used.

.PARAMETER ConnectionProviderName
    Specifies the provider invariant name of the connection string.

.PARAMETER AppDomainBaseDirectory
    Specifies the directory to use for the app-domain that is used for running Migrations
    code such that the app-domain is able to find all required assemblies. This is an
    advanced option that should only be needed if the solution contains	several projects 
    such that the assemblies needed for the context and configuration are not all
    referenced from either the project containing the context or the project containing
    the migrations.
#>
function Get-Migrations
{
    [CmdletBinding(DefaultParameterSetName = 'ConnectionStringName')]
    param (
        [string] $ProjectName,
        [string] $StartUpProjectName,
        [string] $ConfigurationTypeName,
        [parameter(ParameterSetName = 'ConnectionStringName')]
        [string] $ConnectionStringName,
        [parameter(ParameterSetName = 'ConnectionStringAndProviderName',
            Mandatory = $true)]
        [string] $ConnectionString,
        [parameter(ParameterSetName = 'ConnectionStringAndProviderName',
            Mandatory = $true)]
        [string] $ConnectionProviderName,
		[string] $AppDomainBaseDirectory)

    $runner = New-MigrationsRunner $ProjectName $StartUpProjectName $null $ConfigurationTypeName $ConnectionStringName $ConnectionString $ConnectionProviderName $null $AppDomainBaseDirectory

    try
    {
        Invoke-RunnerCommand $runner System.Data.Entity.Migrations.GetMigrationsCommand
        $error = Get-RunnerError $runner
        
        if ($error)
        {
            if ($knownExceptions -notcontains $error.TypeName)
            {
                Write-Host $error.StackTrace
            }
            else
            {
                Write-Verbose $error.StackTrace
            }

            throw $error.Message
        }
    }
    finally
    {
        Remove-Runner $runner
    }
}

function New-MigrationsRunner($ProjectName, $StartUpProjectName, $ContextProjectName, $ConfigurationTypeName, $ConnectionStringName, $ConnectionString, $ConnectionProviderName, $ContextAssemblyName, $AppDomainBaseDirectory)
{
    $startUpProject = Get-MigrationsStartUpProject $StartUpProjectName $ProjectName
    Build-Project $startUpProject

    $project = Get-MigrationsProject $ProjectName
    Build-Project $project

    $contextProject = $project
    if ($ContextProjectName)
    {
        $contextProject = Get-SingleProject $ContextProjectName
        Build-Project $contextProject
    }

    $installPath = Get-EntityFrameworkInstallPath $project
    $toolsPath = Join-Path $installPath tools

    $info = New-AppDomainSetup $project $installPath

    $domain = [AppDomain]::CreateDomain('Migrations', $null, $info)
    $domain.SetData('project', $project)
    $domain.SetData('contextProject', $contextProject)
    $domain.SetData('startUpProject', $startUpProject)
    $domain.SetData('configurationTypeName', $ConfigurationTypeName)
    $domain.SetData('connectionStringName', $ConnectionStringName)
    $domain.SetData('connectionString', $ConnectionString)
    $domain.SetData('connectionProviderName', $ConnectionProviderName)
    $domain.SetData('contextAssemblyName', $ContextAssemblyName)
    $domain.SetData('appDomainBaseDirectory', $AppDomainBaseDirectory)
    
    $dispatcher = New-DomainDispatcher $toolsPath
    $domain.SetData('efDispatcher', $dispatcher)

    return @{
        Domain = $domain;
        ToolsPath = $toolsPath
    }
}

function New-EFConfigRunner($Project)
{
    $installPath = Get-EntityFrameworkInstallPath $Project
    $toolsPath = Join-Path $installPath tools
    $info = New-AppDomainSetup $Project $installPath

    $domain = [AppDomain]::CreateDomain('EFConfig', $null, $info)
    $domain.SetData('project', $Project)
    
    $dispatcher = New-DomainDispatcher $toolsPath
    $domain.SetData('efDispatcher', $dispatcher)

    return @{
        Domain = $domain;
        ToolsPath = $toolsPath
    }
}

function New-AppDomainSetup($Project, $InstallPath)
{
    $info = New-Object System.AppDomainSetup -Property @{
            ShadowCopyFiles = 'true';
            ApplicationBase = $InstallPath;
            PrivateBinPath = 'tools';
            ConfigurationFile = ([AppDomain]::CurrentDomain.SetupInformation.ConfigurationFile)
        }
    
    $targetFrameworkVersion = (New-Object System.Runtime.Versioning.FrameworkName ($Project.Properties.Item('TargetFrameworkMoniker').Value)).Version

    if ($targetFrameworkVersion -lt (New-Object Version @( 4, 5 )))
    {
        $info.PrivateBinPath += ';lib\net40'
    }
    else
    {
        $info.PrivateBinPath += ';lib\net45'
    }

    return $info
}

function New-DomainDispatcher($ToolsPath)
{
    $utilityAssembly = [System.Reflection.Assembly]::LoadFrom((Join-Path $ToolsPath EntityFramework.PowerShell.Utility.dll))
    $dispatcher = $utilityAssembly.CreateInstance(
        'System.Data.Entity.Migrations.Utilities.DomainDispatcher',
        $false,
        [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::Public,
        $null,
        $PSCmdlet,
        $null,
        $null)

    return $dispatcher
}

function Remove-Runner($runner)
{
    [AppDomain]::Unload($runner.Domain)
}

function Invoke-RunnerCommand($runner, $command, $parameters, $anonymousArguments)
{
    $domain = $runner.Domain

    if ($anonymousArguments)
    {
        $anonymousArguments.GetEnumerator() | %{
            $domain.SetData($_.Name, $_.Value)
        }
    }

    $domain.CreateInstanceFrom(
        (Join-Path $runner.ToolsPath EntityFramework.PowerShell.dll),
        $command,
        $false,
        0,
        $null,
        $parameters,
        $null,
        $null) | Out-Null
}

function Get-RunnerError($runner)
{
    $domain = $runner.Domain

    if (!$domain.GetData('wasError'))
    {
        return $null
    }

    return @{
            Message = $domain.GetData('error.Message');
            TypeName = $domain.GetData('error.TypeName');
            StackTrace = $domain.GetData('error.StackTrace')
    }
}

function Get-MigrationsProject($name, $hideMessage)
{
    if ($name)
    {
        return Get-SingleProject $name
    }

    $project = Get-Project
    $projectName = $project.Name

    if (!$hideMessage)
    {
        Write-Verbose "Using NuGet project '$projectName'."
    }

    return $project
}

function Get-MigrationsStartUpProject($name, $fallbackName)
{    
    $startUpProject = $null

    if ($name)
    {
        $startUpProject = Get-SingleProject $name
    }
    else
    {
        $startupProjectPaths = $DTE.Solution.SolutionBuild.StartupProjects

        if ($startupProjectPaths)
        {
            if ($startupProjectPaths.Length -eq 1)
            {
                $startupProjectPath = $startupProjectPaths[0]

                if (!(Split-Path -IsAbsolute $startupProjectPath))
                {
                    $solutionPath = Split-Path $DTE.Solution.Properties.Item('Path').Value
                    $startupProjectPath = Join-Path $solutionPath $startupProjectPath -Resolve
                }

                $startupProject = Get-SolutionProjects | ?{
                    try
                    {
                        $fullName = $_.FullName
                    }
                    catch [NotImplementedException]
                    {
                        return $false
                    }

                    if ($fullName -and $fullName.EndsWith('\'))
                    {
                        $fullName = $fullName.Substring(0, $fullName.Length - 1)
                    }

                    return $fullName -eq $startupProjectPath
                }
            }
            else
            {
                Write-Verbose 'More than one start-up project found.'
            }
        }
        else
        {
            Write-Verbose 'No start-up project found.'
        }
    }

    if (!($startUpProject -and (Test-StartUpProject $startUpProject)))
    {
        $startUpProject = Get-MigrationsProject $fallbackName $true
        $startUpProjectName = $startUpProject.Name

        Write-Warning "Cannot determine a valid start-up project. Using project '$startUpProjectName' instead. Your configuration file and working directory may not be set as expected. Use the -StartUpProjectName parameter to set one explicitly. Use the -Verbose switch for more information."
    }
    else
    {
        $startUpProjectName = $startUpProject.Name

        Write-Verbose "Using StartUp project '$startUpProjectName'."
    }

    return $startUpProject
}

function Get-SolutionProjects()
{
    $projects = New-Object System.Collections.Stack
    
    $DTE.Solution.Projects | %{
        $projects.Push($_)
    }
    
    while ($projects.Count -ne 0)
    {
        $project = $projects.Pop();
        
        # NOTE: This line is similar to doing a "yield return" in C#
        $project

        if ($project.ProjectItems)
        {
            $project.ProjectItems | ?{ $_.SubProject } | %{
                $projects.Push($_.SubProject)
            }
        }
    }
}

function Get-SingleProject($name)
{
    $project = Get-Project $name

    if ($project -is [array])
    {
        throw "More than one project '$name' was found. Specify the full name of the one to use."
    }

    return $project
}

function Test-StartUpProject($project)
{    
    if ($project.Kind -eq '{cc5fd16d-436d-48ad-a40c-5a424c6e3e79}')
    {
        $projectName = $project.Name
        Write-Verbose "Cannot use start-up project '$projectName'. The Windows Azure Project type isn't supported."
        
        return $false
    }
    
    return $true
}

function Build-Project($project)
{
    $configuration = $DTE.Solution.SolutionBuild.ActiveConfiguration.Name

    $DTE.Solution.SolutionBuild.BuildProject($configuration, $project.UniqueName, $true)

    if ($DTE.Solution.SolutionBuild.LastBuildInfo)
    {
        $projectName = $project.Name

        throw "The project '$projectName' failed to build."
    }
}

function Get-EntityFrameworkInstallPath($project)
{
    $package = Get-Package -ProjectName $project.FullName | ?{ $_.Id -eq 'EntityFramework' }

    if (!$package)
    {
        $projectName = $project.Name

        throw "The EntityFramework package is not installed on project '$projectName'."
    }
    
    return Get-PackageInstallPath $package
}
    
function Get-PackageInstallPath($package)
{
    $componentModel = Get-VsComponentModel
    $packageInstallerServices = $componentModel.GetService([NuGet.VisualStudio.IVsPackageInstallerServices])

    $vsPackage = $packageInstallerServices.GetInstalledPackages() | ?{ $_.Id -eq $package.Id -and $_.Version -eq $package.Version }
    
    return $vsPackage.InstallPath
}

function Check-Project($project)
{
    if (!$project.FullName)
    {
        throw "The Project argument must refer to a Visual Studio project. Use the '`$project' variable provided by NuGet when running in install.ps1."
    }

	return $project.CodeModel
}

function Hint-Downgrade ($name) {
    if (Get-Module | Where { $_.Name -eq 'EntityFrameworkCore' }) {
        Write-Warning "Both Entity Framework 6.x and Entity Framework Core commands are installed. The Entity Framework 6 version is executing. You can fully qualify the command to select which one to execute, 'EntityFramework\$name' for EF6.x and 'EntityFrameworkCore\$name' for EF Core."
    }
}

Export-ModuleMember @( 'Enable-Migrations', 'Add-Migration', 'Update-Database', 'Get-Migrations', 'Add-EFProvider', 'Add-EFDefaultConnectionFactory', 'Initialize-EFConfiguration') -Variable InitialDatabase

# SIG # Begin signature block
# MIIa3gYJKoZIhvcNAQcCoIIazzCCGssCAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQUMst5oGNhRFW5FyGr05jAUKeC
# HF6gghWAMIIEwjCCA6qgAwIBAgITMwAAAL+RbPt8GiTgIgAAAAAAvzANBgkqhkiG
# 9w0BAQUFADB3MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4G
# A1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSEw
# HwYDVQQDExhNaWNyb3NvZnQgVGltZS1TdGFtcCBQQ0EwHhcNMTYwOTA3MTc1ODQ5
# WhcNMTgwOTA3MTc1ODQ5WjCBsjELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hp
# bmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jw
# b3JhdGlvbjEMMAoGA1UECxMDQU9DMScwJQYDVQQLEx5uQ2lwaGVyIERTRSBFU046
# NTdDOC0yRDE1LTFDOEIxJTAjBgNVBAMTHE1pY3Jvc29mdCBUaW1lLVN0YW1wIFNl
# cnZpY2UwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCt7X+GwPaidVcV
# TRT2yohV/L1dpTMCvf4DHlCY0GUmhEzD4Yn22q/qnqZTHDd8IlI/OHvKhWC9ksKE
# F+BgBHtUQPSg7s6+ZXy69qX64r6m7X/NYizeK31DsScLsDHnqsbnwJaNZ2C2u5hh
# cKsHvc8BaSsv/nKlr6+eg2iX2y9ai1uB1ySNeunEtdfchAr1U6Qb7AJHrXMTdKl8
# ptLov67aFU0rRRMwQJOWHR+o/gQa9v4z/f43RY2PnMRoF7Dztn6ditoQ9CgTiMdS
# MtsqFWMAQNMt5bZ8oY1hmgkSDN6FwTjVyUEE6t3KJtgX2hMHjOVqtHXQlud0GR3Z
# LtAOMbS7AgMBAAGjggEJMIIBBTAdBgNVHQ4EFgQU5GwaORrHk1i0RjZlB8QAt3kX
# nBEwHwYDVR0jBBgwFoAUIzT42VJGcArtQPt2+7MrsMM1sw8wVAYDVR0fBE0wSzBJ
# oEegRYZDaHR0cDovL2NybC5taWNyb3NvZnQuY29tL3BraS9jcmwvcHJvZHVjdHMv
# TWljcm9zb2Z0VGltZVN0YW1wUENBLmNybDBYBggrBgEFBQcBAQRMMEowSAYIKwYB
# BQUHMAKGPGh0dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9wa2kvY2VydHMvTWljcm9z
# b2Z0VGltZVN0YW1wUENBLmNydDATBgNVHSUEDDAKBggrBgEFBQcDCDANBgkqhkiG
# 9w0BAQUFAAOCAQEAjt62jcZ+2YBqm7RKit827DRU9OKioi6HEERT0X0bL+JjUTu3
# 7k4piPcK3J/0cfktWuPjrYSuySa/NbkmlvAhQV4VpoWxipx3cZplF9HK9IH4t8AD
# YDxUI5u1xb2r24aExGIzWY+1uH92bzTKbAjuwNzTMQ1z10Kca4XXPI4HFZalXxgL
# fbjCkV3IKNspU1TILV0Dzk0tdKAwx/MoeZN1HFcB9WjzbpFnCVH+Oy/NyeJOyiNE
# 4uT/6iyHz1+XCqf2nIrV/DXXsJYKwifVlOvSJ4ZrV40MYucq3lWQuKERfXivLFXl
# dKyXQrS4eeToRPSevRisc0GBYuZczpkdeN5faDCCBOswggPToAMCAQICEzMAAAF4
# JVq1zSPGX5UAAQAAAXgwDQYJKoZIhvcNAQEFBQAweTELMAkGA1UEBhMCVVMxEzAR
# BgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1p
# Y3Jvc29mdCBDb3Jwb3JhdGlvbjEjMCEGA1UEAxMaTWljcm9zb2Z0IENvZGUgU2ln
# bmluZyBQQ0EwHhcNMTcwODExMjAxMTE1WhcNMTgwODExMjAxMTE1WjCBgjELMAkG
# A1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQx
# HjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEMMAoGA1UECxMDQU9DMR4w
# HAYDVQQDExVNaWNyb3NvZnQgQ29ycG9yYXRpb24wggEiMA0GCSqGSIb3DQEBAQUA
# A4IBDwAwggEKAoIBAQCZbh1TVaudsrIbXUPPB9c8S+E+dKSbskHKYlG6SGTH8jhT
# hpuvGiAO87F2b9GHVN+DvszaMkGy/xQgHaGEJLfpnb3kpakic7E0bjDHdG4KnHRb
# no/wfUkGLfS79o+cw//RY8Ck6yE+0czDBcxp0Gbw5JyGP+KFqvzRR/3Tv3nt/5x0
# 5ZnEOHYP+eDVikDvn/DH+oxxtiCfX3tkvtm/yX4eOb47YdmYKQjCgz2+Nil/lupY
# vU0QFIjvke3jshqQINDng/vO9ys2qA0ex/q5hlLKQTST99dGoM86Ge6F723ReToq
# KnGLN8kiCG7uNapOAIQrpCHZq96CVumiaA5ZvxU9AgMBAAGjggFgMIIBXDATBgNV
# HSUEDDAKBggrBgEFBQcDAzAdBgNVHQ4EFgQUjuhtD3FD7tk/RKloJFX05cpgLjcw
# UQYDVR0RBEowSKRGMEQxDDAKBgNVBAsTA0FPQzE0MDIGA1UEBRMrMjI5ODAzKzFh
# YmY5ZTVmLWNlZDAtNDJlNi1hNjVkLWQ5MzUwOTU5ZmUwZTAfBgNVHSMEGDAWgBTL
# EejK0rQWWAHJNy4zFha5TJoKHzBWBgNVHR8ETzBNMEugSaBHhkVodHRwOi8vY3Js
# Lm1pY3Jvc29mdC5jb20vcGtpL2NybC9wcm9kdWN0cy9NaWNDb2RTaWdQQ0FfMDgt
# MzEtMjAxMC5jcmwwWgYIKwYBBQUHAQEETjBMMEoGCCsGAQUFBzAChj5odHRwOi8v
# d3d3Lm1pY3Jvc29mdC5jb20vcGtpL2NlcnRzL01pY0NvZFNpZ1BDQV8wOC0zMS0y
# MDEwLmNydDANBgkqhkiG9w0BAQUFAAOCAQEAYnG/oHG/xgZYR8NAMHZ/vE9GM0e4
# 7YdhuTea2uY7pSGwM707wp8Wan0Fa6evK1PWfcd/XNOh2BpEv5o8RmKDoEsG0ECP
# 13Jug7cklfKreBVHQ+Djg43VVFLZpuo7aOAVK6wjlcnpPUtn+SfH9K0aM2FjDKVJ
# FW6XFKXBat5R+Zp6uOxWTxpSeMTeDC5zF6IY6ogR1uzU+9EQoRlAvkwX6po+exEL
# nMLr4++P+fqOxIU+PODIoB8ijClAqwwKvLlMPa3qlrNHt+LweTMu7lvGC/RA18wU
# zzXAeomuZ03blUw+bkOiVgWOk4S0RN7EnW7zjJV8gd/+G2dbToUi1cB/fTCCBbww
# ggOkoAMCAQICCmEzJhoAAAAAADEwDQYJKoZIhvcNAQEFBQAwXzETMBEGCgmSJomT
# 8ixkARkWA2NvbTEZMBcGCgmSJomT8ixkARkWCW1pY3Jvc29mdDEtMCsGA1UEAxMk
# TWljcm9zb2Z0IFJvb3QgQ2VydGlmaWNhdGUgQXV0aG9yaXR5MB4XDTEwMDgzMTIy
# MTkzMloXDTIwMDgzMTIyMjkzMloweTELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldh
# c2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBD
# b3Jwb3JhdGlvbjEjMCEGA1UEAxMaTWljcm9zb2Z0IENvZGUgU2lnbmluZyBQQ0Ew
# ggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCycllcGTBkvx2aYCAgQpl2
# U2w+G9ZvzMvx6mv+lxYQ4N86dIMaty+gMuz/3sJCTiPVcgDbNVcKicquIEn08Gis
# TUuNpb15S3GbRwfa/SXfnXWIz6pzRH/XgdvzvfI2pMlcRdyvrT3gKGiXGqelcnNW
# 8ReU5P01lHKg1nZfHndFg4U4FtBzWwW6Z1KNpbJpL9oZC/6SdCnidi9U3RQwWfjS
# jWL9y8lfRjFQuScT5EAwz3IpECgixzdOPaAyPZDNoTgGhVxOVoIoKgUyt0vXT2Pn
# 0i1i8UU956wIAPZGoZ7RW4wmU+h6qkryRs83PDietHdcpReejcsRj1Y8wawJXwPT
# AgMBAAGjggFeMIIBWjAPBgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBTLEejK0rQW
# WAHJNy4zFha5TJoKHzALBgNVHQ8EBAMCAYYwEgYJKwYBBAGCNxUBBAUCAwEAATAj
# BgkrBgEEAYI3FQIEFgQU/dExTtMmipXhmGA7qDFvpjy82C0wGQYJKwYBBAGCNxQC
# BAweCgBTAHUAYgBDAEEwHwYDVR0jBBgwFoAUDqyCYEBWJ5flJRP8KuEKU5VZ5KQw
# UAYDVR0fBEkwRzBFoEOgQYY/aHR0cDovL2NybC5taWNyb3NvZnQuY29tL3BraS9j
# cmwvcHJvZHVjdHMvbWljcm9zb2Z0cm9vdGNlcnQuY3JsMFQGCCsGAQUFBwEBBEgw
# RjBEBggrBgEFBQcwAoY4aHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraS9jZXJ0
# cy9NaWNyb3NvZnRSb290Q2VydC5jcnQwDQYJKoZIhvcNAQEFBQADggIBAFk5Pn8m
# Rq/rb0CxMrVq6w4vbqhJ9+tfde1MOy3XQ60L/svpLTGjI8x8UJiAIV2sPS9MuqKo
# VpzjcLu4tPh5tUly9z7qQX/K4QwXaculnCAt+gtQxFbNLeNK0rxw56gNogOlVuC4
# iktX8pVCnPHz7+7jhh80PLhWmvBTI4UqpIIck+KUBx3y4k74jKHK6BOlkU7IG9KP
# cpUqcW2bGvgc8FPWZ8wi/1wdzaKMvSeyeWNWRKJRzfnpo1hW3ZsCRUQvX/TartSC
# Mm78pJUT5Otp56miLL7IKxAOZY6Z2/Wi+hImCWU4lPF6H0q70eFW6NB4lhhcyTUW
# X92THUmOLb6tNEQc7hAVGgBd3TVbIc6YxwnuhQ6MT20OE049fClInHLR82zKwexw
# o1eSV32UjaAbSANa98+jZwp0pTbtLS8XyOZyNxL0b7E8Z4L5UrKNMxZlHg6K3RDe
# ZPRvzkbU0xfpecQEtNP7LN8fip6sCvsTJ0Ct5PnhqX9GuwdgR2VgQE6wQuxO7bN2
# edgKNAltHIAxH+IOVN3lofvlRxCtZJj/UBYufL8FIXrilUEnacOTj5XJjdibIa4N
# XJzwoq6GaIMMai27dmsAHZat8hZ79haDJLmIz2qoRzEvmtzjcT3XAH5iR9HOiMm4
# GPoOco3Boz2vAkBq/2mbluIQqBC0N1AI1sM9MIIGBzCCA++gAwIBAgIKYRZoNAAA
# AAAAHDANBgkqhkiG9w0BAQUFADBfMRMwEQYKCZImiZPyLGQBGRYDY29tMRkwFwYK
# CZImiZPyLGQBGRYJbWljcm9zb2Z0MS0wKwYDVQQDEyRNaWNyb3NvZnQgUm9vdCBD
# ZXJ0aWZpY2F0ZSBBdXRob3JpdHkwHhcNMDcwNDAzMTI1MzA5WhcNMjEwNDAzMTMw
# MzA5WjB3MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UE
# BxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSEwHwYD
# VQQDExhNaWNyb3NvZnQgVGltZS1TdGFtcCBQQ0EwggEiMA0GCSqGSIb3DQEBAQUA
# A4IBDwAwggEKAoIBAQCfoWyx39tIkip8ay4Z4b3i48WZUSNQrc7dGE4kD+7Rp9FM
# rXQwIBHrB9VUlRVJlBtCkq6YXDAm2gBr6Hu97IkHD/cOBJjwicwfyzMkh53y9Gcc
# LPx754gd6udOo6HBI1PKjfpFzwnQXq/QsEIEovmmbJNn1yjcRlOwhtDlKEYuJ6yG
# T1VSDOQDLPtqkJAwbofzWTCd+n7Wl7PoIZd++NIT8wi3U21StEWQn0gASkdmEScp
# ZqiX5NMGgUqi+YSnEUcUCYKfhO1VeP4Bmh1QCIUAEDBG7bfeI0a7xC1Un68eeEEx
# d8yb3zuDk6FhArUdDbH895uyAc4iS1T/+QXDwiALAgMBAAGjggGrMIIBpzAPBgNV
# HRMBAf8EBTADAQH/MB0GA1UdDgQWBBQjNPjZUkZwCu1A+3b7syuwwzWzDzALBgNV
# HQ8EBAMCAYYwEAYJKwYBBAGCNxUBBAMCAQAwgZgGA1UdIwSBkDCBjYAUDqyCYEBW
# J5flJRP8KuEKU5VZ5KShY6RhMF8xEzARBgoJkiaJk/IsZAEZFgNjb20xGTAXBgoJ
# kiaJk/IsZAEZFgltaWNyb3NvZnQxLTArBgNVBAMTJE1pY3Jvc29mdCBSb290IENl
# cnRpZmljYXRlIEF1dGhvcml0eYIQea0WoUqgpa1Mc1j0BxMuZTBQBgNVHR8ESTBH
# MEWgQ6BBhj9odHRwOi8vY3JsLm1pY3Jvc29mdC5jb20vcGtpL2NybC9wcm9kdWN0
# cy9taWNyb3NvZnRyb290Y2VydC5jcmwwVAYIKwYBBQUHAQEESDBGMEQGCCsGAQUF
# BzAChjhodHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtpL2NlcnRzL01pY3Jvc29m
# dFJvb3RDZXJ0LmNydDATBgNVHSUEDDAKBggrBgEFBQcDCDANBgkqhkiG9w0BAQUF
# AAOCAgEAEJeKw1wDRDbd6bStd9vOeVFNAbEudHFbbQwTq86+e4+4LtQSooxtYrhX
# AstOIBNQmd16QOJXu69YmhzhHQGGrLt48ovQ7DsB7uK+jwoFyI1I4vBTFd1Pq5Lk
# 541q1YDB5pTyBi+FA+mRKiQicPv2/OR4mS4N9wficLwYTp2OawpylbihOZxnLcVR
# DupiXD8WmIsgP+IHGjL5zDFKdjE9K3ILyOpwPf+FChPfwgphjvDXuBfrTot/xTUr
# XqO/67x9C0J71FNyIe4wyrt4ZVxbARcKFA7S2hSY9Ty5ZlizLS/n+YWGzFFW6J1w
# lGysOUzU9nm/qhh6YinvopspNAZ3GmLJPR5tH4LwC8csu89Ds+X57H2146SodDW4
# TsVxIxImdgs8UoxxWkZDFLyzs7BNZ8ifQv+AeSGAnhUwZuhCEl4ayJ4iIdBD6Svp
# u/RIzCzU2DKATCYqSCRfWupW76bemZ3KOm+9gSd0BhHudiG/m4LBJ1S2sWo9iaF2
# YbRuoROmv6pH8BJv/YoybLL+31HIjCPJZr2dHYcSZAI9La9Zj7jkIeW1sMpjtHhU
# BdRBLlCslLCleKuzoJZ1GtmShxN1Ii8yqAhuoFuMJb+g74TKIdbrHk/Jmu5J4PcB
# ZW+JC33Iacjmbuqnl84xKf8OxVtc2E0bodj6L54/LlUWa8kTo/0xggTIMIIExAIB
# ATCBkDB5MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UE
# BxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSMwIQYD
# VQQDExpNaWNyb3NvZnQgQ29kZSBTaWduaW5nIFBDQQITMwAAAXglWrXNI8ZflQAB
# AAABeDAJBgUrDgMCGgUAoIHhMBkGCSqGSIb3DQEJAzEMBgorBgEEAYI3AgEEMBwG
# CisGAQQBgjcCAQsxDjAMBgorBgEEAYI3AgEVMCMGCSqGSIb3DQEJBDEWBBTlz9AL
# f1sBglUb85Teir2bIYmVvDCBgAYKKwYBBAGCNwIBDDFyMHCgUoBQAEUAbgB0AGkA
# dAB5ACAARgByAGEAbQBlAHcAbwByAGsAIABUAG8AbwBsAHMAIABmAG8AcgAgAFYA
# aQBzAHUAYQBsACAAUwB0AHUAZABpAG+hGoAYaHR0cDovL21zZG4uY29tL2RhdGEv
# ZWYgMA0GCSqGSIb3DQEBAQUABIIBAGeaKWq3ThdTiw0FniJGaIvelS5BghhhNI3x
# ezUDCIPfp6Glc9zffEPt3qMdZORjY3mgsbrRO7hXNm9GR5cb68G5C7/LhrWfp34J
# yWQ1dbnApNi6bp64SEML52vcDVMMHnbKRM3ns0/y/qu/XH2GSUUPuEjD7cd5t0NO
# DUQZ2gHt4JeqZERgvNQWTxdUn83V2BwOz1i6KtHzmGdvEjb8ZrPQTlu1UoHHYXsQ
# jUv8dhEHkaj3o2jLEIhYXp1ait0/OAk9dA5QdPVCBHy4RkDLWI4v58s0j1J0Cw0l
# 2a/7+Agm1vvVLQZHofllOSIgd5Aj4Y1NiMYpLo8Y4z3iBRXgUQKhggIoMIICJAYJ
# KoZIhvcNAQkGMYICFTCCAhECAQEwgY4wdzELMAkGA1UEBhMCVVMxEzARBgNVBAgT
# Cldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29m
# dCBDb3Jwb3JhdGlvbjEhMB8GA1UEAxMYTWljcm9zb2Z0IFRpbWUtU3RhbXAgUENB
# AhMzAAAAv5Fs+3waJOAiAAAAAAC/MAkGBSsOAwIaBQCgXTAYBgkqhkiG9w0BCQMx
# CwYJKoZIhvcNAQcBMBwGCSqGSIb3DQEJBTEPFw0xNzEwMjMyMDEzNDhaMCMGCSqG
# SIb3DQEJBDEWBBTDiJZ8a7P4Som1wzlUt7g8OiC3qzANBgkqhkiG9w0BAQUFAASC
# AQAmJlbq0GKEk6SUOqA4Ksyj99KUlnPFcFyf1AqvHRdShmQjfEEo/pmR9cWmGNdA
# 8M7wVFFZFRlmkF3oCl//ckniUJ/aDYtXCTskDFd2Tc8xJ/nsJOespt0LSpbiadxI
# Goa9nZNXymAEidosoh0MmqB25gQfq9X+mMU7rVUp9eBUjAcoT3FHv+gkzE1XjvDE
# YFGvQ1PRxjiYKlQKITDhtFhog2MSuGtvWpFV1aeqXDrANONlLyrscHrzKjQkKiwU
# PPJQ+iygds9A3xUri6Jevj7Yjvr0hBTFaxL7xmQtgaafY/TqAtRcbCz2Gu9hkUay
# WYGdtIZc7fAdNGkxqRxddvkD
# SIG # End signature block
