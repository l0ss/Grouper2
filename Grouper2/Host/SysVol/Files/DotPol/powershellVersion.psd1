###########################################################
#
#  Group Policy - Registry Policy parser module
#
#  Copyright (c) Microsoft Corporation, 2016
#
###########################################################

data LocalizedData
{
    # culture="en-US"
    ConvertFrom-StringData @'
    InvalidHeader = File '{0}' has an invalid header.
    InvalidVersion = File '{0}' has an invalid version. It should be 1.
    InvalidFormatBracket = File '{0}' has an invalid format. A [ or ] was expected at location {1}.
    InvalidFormatSemicolon = File '{0}' has an invalid format. A ; was expected at location {1}.
    OnlyCreatingKey = Some values are null. Only the registry key is created.
    InvalidPath = Path {0} doesn't point to an existing registry key/property.
    InternalError = Internal error while creating a registry entry for {0}
    InvalidIntegerSize = Invalid size for an integer. Must be less than or equal to 8.
'@
}

Import-LocalizedData  LocalizedData -filename GPRegistryPolicyParser.Strings.psd1

$script:REGFILE_SIGNATURE = 0x67655250 # PRef
$script:REGISTRY_FILE_VERSION = 0x00000001 #Initially defined as 1, then incremented each time the file format is changed.

$script:DefaultEntries = @(
    "Software\Policies"
)

Enum RegType {
    REG_NONE                       = 0	# No value type
    REG_SZ                         = 1	# Unicode null terminated string
    REG_EXPAND_SZ                  = 2	# Unicode null terminated string (with environmental variable references)
    REG_BINARY                     = 3	# Free form binary
    REG_DWORD                      = 4	# 32-bit number
    REG_DWORD_LITTLE_ENDIAN        = 4	# 32-bit number (same as REG_DWORD)
    REG_DWORD_BIG_ENDIAN           = 5	# 32-bit number
    REG_LINK                       = 6	# Symbolic link (Unicode)
    REG_MULTI_SZ                   = 7	# Multiple Unicode strings, delimited by \0, terminated by \0\0
    REG_RESOURCE_LIST              = 8  # Resource list in resource map
    REG_FULL_RESOURCE_DESCRIPTOR   = 9  # Resource list in hardware description
    REG_RESOURCE_REQUIREMENTS_LIST = 10
    REG_QWORD                      = 11 # 64-bit number
    REG_QWORD_LITTLE_ENDIAN        = 11 # 64-bit number (same as REG_QWORD)
}

Class GPRegistryPolicy
{
    [string]  $KeyName
    [string]  $ValueName
    [RegType] $ValueType
    [string]  $ValueLength
    [object]  $ValueData

    GPRegistryPolicy()
    {
        $this.KeyName     = $Null
        $this.ValueName   = $Null
        $this.ValueType   = [RegType]::REG_NONE
        $this.ValueLength = 0
        $this.ValueData   = $Null
    }

    GPRegistryPolicy(
            [string]  $KeyName,
            [string]  $ValueName,
            [RegType] $ValueType,
            [string]  $ValueLength,
            [object]  $ValueData
        )
    {
        $this.KeyName     = $KeyName
        $this.ValueName   = $ValueName
        $this.ValueType   = $ValueType
        $this.ValueLength = $ValueLength
        $this.ValueData   = $ValueData
    }

    [string] GetRegTypeString()
    {
        [string] $Result = ""

        switch ($this.ValueType)
        {
            ([RegType]::REG_SZ)        { $Result = "String" }
            ([RegType]::REG_EXPAND_SZ) { $Result = "ExpandString" }
            ([RegType]::REG_BINARY)    { $Result = "Binary" }
            ([RegType]::REG_DWORD)     { $Result = "DWord" }
            ([RegType]::REG_MULTI_SZ)  { $Result = "MultiString" }
            ([RegType]::REG_QWORD)     { $Result = "QWord" }
            default                    { $Result = "" }
        }

        return $Result
    }

    static [RegType] GetRegTypeFromString( [string] $Type )
    {
        $Result = [RegType]::REG_NONE

        switch ($Type)
        {
            "String"       { $Result = [RegType]::REG_SZ }
            "ExpandString" { $Result = [RegType]::REG_EXPAND_SZ }
            "Binary"       { $Result = [RegType]::REG_BINARY }
            "DWord"        { $Result = [RegType]::REG_DWORD }
            "MultiString"  { $Result = [RegType]::REG_MULTI_SZ }
            "QWord"        { $Result = [RegType]::REG_QWORD }
            default        { $Result = [RegType]::REG_NONE }
        }

        return $Result
    }
}

Function New-GPRegistryPolicy
{
    param (
        [Parameter(Mandatory=$true,Position=0)]
        [ValidateNotNullOrEmpty()]
        [string]
        $keyName,
        
        [Parameter(Position=1)]
        [string]
        $valueName = $null,
        
        [Parameter(Position=2)]
        [RegType]
        $valueType = [RegType]::REG_NONE,
        
        [Parameter(Position=3)]
        [string]
        $valueLength = $null,
        
        [Parameter(Position=4)]
        [object]
        $valueData = $null
        )

    $Policy = [GPRegistryPolicy]::new($keyName, $valueName, $valueType, $valueLength, $valueData)

    return $Policy;
}

Function Get-RegType
{
    param (
		[Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Type
    )

    return [GPRegistryPolicy]::GetRegTypeFromString($Type)
}

<# 
.SYNOPSIS
Reads and parses a .pol file.

.DESCRIPTION
Reads a .pol file, parses it and returns an array of Group Policy registry settings.

.PARAMETER Path
Specifies the path to the .pol file.

.EXAMPLE
C:\PS> Parse-PolFile -Path "C:\Registry.pol"
#>
Function Parse-PolFile
{
    [OutputType([Array])]
    param (
        [Parameter(Mandatory=$true,Position=0)]
        [string]
        $Path
    )

    [Array] $RegistryPolicies = @()
    $index = 0

    [string] $policyContents = Get-Content $Path -Raw
    [byte[]] $policyContentInBytes = Get-Content $Path -Raw -Encoding Byte

    # 4 bytes are the signature PReg
    $signature = [System.Text.Encoding]::ASCII.GetString($policyContents[0..3])
    $index += 4
    Assert ($signature -eq 'PReg') ($LocalizedData.InvalidHeader -f $Path)

    # 4 bytes are the version
    $version = [System.BitConverter]::ToInt32($policyContentInBytes, 4)
    $index += 4
    Assert ($version -eq 1) ($LocalizedData.InvalidVersion -f $Path)

    # Start processing at byte 8
    while($index -lt $policyContents.Length - 2)
    {
        [string]$keyName = $null
        [string]$valueName = $null
        [int]$valueType = $null
        [int]$valueLength = $null

        [object]$value = $null

        # Next UNICODE character should be a [
        $leftbracket = [System.BitConverter]::ToChar($policyContentInBytes, $index)
        Assert ($leftbracket -eq '[') "Missing the openning bracket"
        $index+=2

        # Next UNICODE string will continue until the ; less the null terminator
        $semicolon = $policyContents.IndexOf(";", $index)
        Assert ($semicolon -ge 0) "Failed to locate the semicolon after key name."
        $keyName = [System.Text.Encoding]::UNICODE.GetString($policyContents[($index)..($semicolon-3)]) # -3 to exclude the null termination and ';' characters
        $index = $semicolon + 2

        # Next UNICODE string will continue until the ; less the null terminator
        $semicolon = $policyContents.IndexOf(";", $index)
        Assert ($semicolon -ge 0) "Failed to locate the semicolon after value name."
        $valueName = [System.Text.Encoding]::UNICODE.GetString($policyContents[($index)..($semicolon-3)]) # -3 to exclude the null termination and ';' characters
        $index = $semicolon + 2

        # Next DWORD will continue until the ;
        $semicolon = $index + 4 # DWORD Size
        Assert ([System.BitConverter]::ToChar($policyContentInBytes, $semicolon) -eq ';') "Failed to locate the semicolon after value type."
        $valueType = [System.BitConverter]::ToInt32($policyContentInBytes, $index)
        $index=$semicolon + 2 # Skip ';'

        # Next DWORD will continue until the ;
        $semicolon = $index + 4 # DWORD Size
        Assert ([System.BitConverter]::ToChar($policyContentInBytes, $semicolon) -eq ';') "Failed to locate the semicolon after value length."
        $valueLength = Convert-StringToInt -ValueString $policyContentInBytes[$index..($index+3)]
        $index=$semicolon + 2 # Skip ';'

        if ($valueLength -gt 0)
        {
            # String types less the null terminator for REG_SZ and REG_EXPAND_SZ
            # REG_SZ: string type (ASCII)
            if($valueType -eq [RegType]::REG_SZ)
            {
                [string] $value = [System.Text.Encoding]::UNICODE.GetString($policyContents[($index)..($index+$valueLength-3)]) # -3 to exclude the null termination and ']' characters
                $index += $valueLength
            }

            # REG_EXPAND_SZ: string, includes %ENVVAR% (expanded by caller) (ASCII)
            if($valueType -eq [RegType]::REG_EXPAND_SZ)
            {
                [string] $value = [System.Text.Encoding]::UNICODE.GetString($policyContents[($index)..($index+$valueLength-3)]) # -3 to exclude the null termination and ']' characters
                $index += $valueLength
            }

            # For REG_MULTI_SZ leave the last null terminator
            # REG_MULTI_SZ: multiple strings, delimited by \0, terminated by \0\0 (ASCII)
            if($valueType -eq [RegType]::REG_MULTI_SZ)
            {
                [string] $value = [System.Text.Encoding]::UNICODE.GetString($policyContents[($index)..($index+$valueLength-3)])
                $index += $valueLength
            }

            # REG_BINARY: binary values
            if($valueType -eq [RegType]::REG_BINARY)
            {
                [byte[]] $value = $policyContentInBytes[($index)..($index+$valueLength-1)]
                $index += $valueLength
            }
        }

        # DWORD: (4 bytes) in little endian format
        if($valueType -eq [RegType]::REG_DWORD)
        {
            $value = Convert-StringToInt -ValueString $policyContentInBytes[$index..($index+3)]
            $index += 4
        }

        # QWORD: (8 bytes) in little endian format
        if($valueType -eq [RegType]::REG_QWORD)
        {
            $value = Convert-StringToInt -ValueString $policyContentInBytes[$index..($index+7)]
            $index += 8
        }

        # Next UNICODE character should be a ]
        $rightbracket = $policyContents.IndexOf("]", $index) # Skip over null data value if one exists
        Assert ($rightbracket -ge 0) "Missing the closing bracket."
        $index = $rightbracket + 2

        $entry = New-GPRegistryPolicy $keyName $valueName $valueType $valueLength $value

        $RegistryPolicies += $entry
    }

    return $RegistryPolicies
}

<# 
.SYNOPSIS
Reads registry policies from a list of entries.

.DESCRIPTION
Reads registry policies from a list of entries and returns an array of GPRegistryPolicies.

.PARAMETER Division
Specifies the division from which the registry entries will be read.

.EXAMPLE
C:\PS> Read-RegistryPolicies -Division "LocalMachine"

.EXAMPLE
C:\PS> Read-RegistryPolicies -Division "LocalMachine" -Entries @('Software\Policies\Microsoft\Windows', 'Software\Policies\Microsoft\WindowsFirewall')
#>
Function Read-RegistryPolicies
{
    [OutputType([Array])]
    param (

        [ValidateSet("LocalMachine", "CurrentUser", "Users")]
        [string]
        $Division = "LocalMachine",
		
        [string[]]
        $Entries = $script:DefaultEntries
    )

    [Array] $RegistryPolicies = @()

    switch ($Division) 
    { 
        'LocalMachine' { $Hive = [Microsoft.Win32.Registry]::LocalMachine } 
        'CurrentUser'  { $Hive = [Microsoft.Win32.Registry]::CurrentUser } 
        'Users'        { $Hive = [Microsoft.Win32.Registry]::Users } 
    }

    foreach ($entry in $Entries)
    {
        #if (Test-Path -Path $entry)
        if (IsRegistryKey -Path $entry -Hive $Hive)
        {
            # $entry is a key.
            $Key = $Hive.OpenSubKey($entry)

            # Add the key itself
            $rp = New-GPRegistryPolicy -keyName $entry
            $RegistryPolicies += $rp

            # Check default value
            if ($Key.GetValue(''))
            {
                $info = Get-RegKeyInfo -RegKey $Key -ValueName ''
                $rp = New-GPRegistryPolicy -keyName $entry -valueName '' -valueType $info.Type -valueLength $info.Size -valueData $info.Data
                $RegistryPolicies += $rp
            }
            
            if ($Key.ValueCount -gt 0)
            {
                # Copy values under the key
                $ValueNames = $Key.GetValueNames()
                foreach($value in $ValueNames)
                {
                    if ([System.String]::IsNullOrEmpty($value))
                    {
                        $rp = New-GPRegistryPolicy -keyName $entry
                    }
                    else
                    {
                        $info = Get-RegKeyInfo -RegKey $Key -ValueName $value
                        $rp = New-GPRegistryPolicy -keyName $entry -valueName $value -valueType $info.Type -valueLength $info.Size -valueData $info.Data
                    }
                    $RegistryPolicies += $rp
                }
            }

            if ($Key.SubKeyCount -gt 0)
            {
                # Copy subkeys recursively
                $SubKeyNames = $Key.GetSubKeyNames()
                $newEntries = @()

                foreach($subkey in $SubKeyNames)
                {
                    $newEntry = Join-Path -Path $entry -ChildPath $subkey
                    $newEntries += ,$newEntry
                }

                $RegistryPolicies += Read-RegistryPolicies -Entries $newEntries -Division $Division
            }
        }
        else
        {
            $Tokens = $entry.Split('\')
            $Property = $Tokens[-1]
            $ParentKey = $Tokens[0..($Tokens.Count-2)] -join '\'
            $NoSuchKeyOrProperty = $false
        
            if (IsRegistryKey -Path $ParentKey -Hive $Hive)
            {
                # $entry is a property.
                # [key;value;type;size;data]
        
                $Key = $Hive.OpenSubKey($ParentKey)

                if ($Key.GetValueNames() -icontains $Property)
                {
                    $info = Get-RegKeyInfo -RegKey $Key -ValueName $Property
                    $rp = [GPRegistryPolicy]::new($ParentKey, $Property, $info.Type, $info.Size, $info.Data)
                    $RegistryPolicies += $rp
                }
                else
                {
                    $NoSuchKeyOrProperty = $true
                }
            }
            else
            {
                $NoSuchKeyOrProperty = $true
            }

            if ( $NoSuchKeyOrProperty -and @('Continue', 'SilentlyContinue', 'Ignore' ) -inotcontains $ErrorActionPreference)
            {
                # $entry points to a key/property that doesn't exist.
                $NoSuchKeyOrProperty = $true
                Fail -ErrorMessage ($LocalizedData.InvalidPath -f $entry)
            }
        }
    }

    return $RegistryPolicies
}

<# 
.SYNOPSIS
Creates a .pol file entry byte array from a GPRegistryPolicy instance.

.DESCRIPTION
Creates a .pol file entry byte array from a GPRegistryPolicy instance. This entry can be written
in a .pol file later.

.PARAMETER RegistryPolicy
Specifies the registry policy entry.
#>
Function Create-RegistrySettingsEntry
{
    [OutputType([Array])]
    param (
		[Parameter(Mandatory = $true)]
        [alias("RP")]
        [GPRegistryPolicy]
        $RegistryPolicy
    )
        
    # Entry format: [key;value;type;size;data]
    [Byte[]] $Entry = @()
        
    $Entry += [System.Text.Encoding]::Unicode.GetBytes('[') # Openning bracket
        
    $Entry += [System.Text.Encoding]::Unicode.GetBytes($RP.KeyName + "`0")

    $Entry += [System.Text.Encoding]::Unicode.GetBytes(';') # semicolon as delimiter

    $Entry += [System.Text.Encoding]::Unicode.GetBytes($RP.ValueName + "`0")

    $Entry += [System.Text.Encoding]::Unicode.GetBytes(';') # semicolon as delimiter

    $Entry += [System.BitConverter]::GetBytes([Int32]$RP.ValueType)

    $Entry += [System.Text.Encoding]::Unicode.GetBytes(';') # semicolon as delimiter

    #Assert $type ($LocalizedData.InternalError -f $key)
    # Get data bytes then compute byte size based on data and type
    switch ($RP.ValueType)
    {
        { @([RegType]::REG_SZ, [RegType]::REG_EXPAND_SZ, [RegType]::REG_MULTI_SZ) -contains $_ }
            {
                $dataBytes = [System.Text.Encoding]::Unicode.GetBytes($RP.ValueData + "`0")
                $dataSize = $dataBytes.Count
            }

        ([RegType]::REG_BINARY)
            {
                $dataBytes = [System.Text.Encoding]::Unicode.GetBytes($RP.ValueData)
                $dataSize = $dataBytes.Count
            }

        ([RegType]::REG_DWORD)
            {
                $dataBytes = [System.BitConverter]::GetBytes([Int32]$RP.ValueData)
                $dataSize = 4
            }

        ([RegType]::REG_QWORD)
            {
                $dataBytes = [System.BitConverter]::GetBytes([Int64]$RP.ValueData)
                $dataSize = 8
            }

        default
            {
                $dataBytes = [System.Text.Encoding]::Unicode.GetBytes("")
                $dataSize = 0
            }
    }

    #Assert $type ($LocalizedData.InternalError -f $key)
    $Entry += [System.BitConverter]::GetBytes($dataSize)

    $Entry += [System.Text.Encoding]::Unicode.GetBytes(';') # semicolon as delimiter

    #Assert $type ($LocalizedData.InternalError -f $key)
    $Entry += $dataBytes

    $Entry += [System.Text.Encoding]::Unicode.GetBytes(']') # Closing bracket

    return $Entry
}

<# 
.SYNOPSIS
Appends an array of registry policy entries to a file.

.DESCRIPTION
Appends an array of registry policy entries to a file.

.PARAMETER RegistryPolicies
An array of registry policy entries.

.PARAMETER Path
Path to a file (.pol extension)
#>
Function Append-RegistryPolicies
{
    param (
		[Parameter(Mandatory = $true)]
        [GPRegistryPolicy[]]
        $RegistryPolicies,

		[Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path
    )
        
    foreach ($rp in $RegistryPolicies)
    {
        [Byte[]] $Entry = Create-RegistrySettingsEntry -RegistryPolicy $rp
        $Entry | Add-Content -Path $Path -Encoding Byte
    }
}

Function Assert
{
    param (
        [Parameter(Mandatory)]
        $Condition,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ErrorMessage
    )

    if (!$Condition) 
    {
        Fail -ErrorMessage $ErrorMessage;
    }
}

Function Fail
{
    param (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ErrorMessage
    )
  
    throw $ErrorMessage
}

<# 
.SYNOPSIS
Creates a file and initializes it with Group Policy Registry file format signature.

.DESCRIPTION
Creates a file and initializes it with Group Policy Registry file format signature.

.PARAMETER Path
Path to a file (.pol extension)
#>
Function Create-GPRegistryPolicyFile
{
    param (
        [Parameter(Mandatory)]
        $Path
    )

    $null = Remove-Item -Path $Path -Force -Verbose -ErrorAction SilentlyContinue

    New-Item -Path $Path -Force -Verbose -ErrorAction Stop | Out-Null

    [System.BitConverter]::GetBytes($script:REGFILE_SIGNATURE) | Add-Content -Path $Path -Encoding Byte
    [System.BitConverter]::GetBytes($script:REGISTRY_FILE_VERSION) | Add-Content -Path $Path -Encoding Byte
}

<# 
.SYNOPSIS
Returns the type, size and data values of a given registry key.

.DESCRIPTION
Returns the type, size and data values of a given registry key.

.PARAMETER RegKey
Registry Key

.PARAMETER ValueName
The name of the Value under the given registry key
#>
Function Get-RegKeyInfo
{
    param (
		[Parameter(Mandatory = $true)]
        [Microsoft.Win32.RegistryKey]
        $RegKey,

		[Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]
        $ValueName

    )

    switch ($RegKey.GetValueKind($ValueName))
    {
        "String"       {
            $Type = $RegKey.GetValueKind($ValueName)
            $Data = $RegKey.GetValue($ValueName)
            $Size = $Data.Length
        }

        "ExpandString"       {
            $Type = $RegKey.GetValueKind($ValueName)
            $Data = $RegKey.GetValue($ValueName,$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
            $Size = $Data.Length
        }

        "Binary"       {
            $Type = $RegKey.GetValueKind($ValueName)
            $value = $RegKey.GetValue($ValueName)
            $Data = [System.Text.Encoding]::Unicode.GetString($value)
            $Size = $Data.Count
        }

        "DWord"        {
            $Type = $RegKey.GetValueKind($ValueName)
            $Data = $RegKey.GetValue($ValueName)
            $Size = 4
        }

        "MultiString"  {
            $Type = $RegKey.GetValueKind($ValueName)
            $Data = ($RegKey.GetValue($ValueName) -join "`0") + "`0"
            $Size = $Data.Length
        }

        "QWord"        {
            $Type = $RegKey.GetValueKind($ValueName)
            $Data = $RegKey.GetValue($ValueName)
            $Size = 8
        }

        default        {
            $Type = $null
            $Data = $null
            $Size = 0
        }
    }

    return @{
        'Type' = $Type;
        'Size' = $Size;
        'Data' = $Data;
    }
}

Function IsRegistryKey
{
    param (
		[Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path,

        [Microsoft.Win32.RegistryKey]
        $Hive = [Microsoft.Win32.Registry]::LocalMachine
    )

    $key = $Hive.OpenSubKey($Path)

    if ($key)
    {
        if ($PSVersionTable.PSEdition -ieq 'Core')
        {
            $key.Flush()
            $key.Dispose()
        }
        else
        {
            $key.Close()
        }
        return $true
    }
    else
    {
        return $false
    }
}

Function Convert-StringToInt
{
    param (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [System.Object[]]
        $ValueString
    )
  
    if ($ValueString.Length -le 4)
    {
        [int32] $result = 0
    }
    elseif ($ValueString.Length -le 8)
    {
        [int64] $result = 0
    }
    else
    {
        Fail -ErrorMessage $LocalizedData.InvalidIntegerSize
    }

    for ($i = $ValueString.Length - 1 ; $i -ge 0 ; $i -= 1)
    {
        $result = $result -shl 8
        $result = $result + ([int][char]$ValueString[$i])
    }

    return $result
}

Export-ModuleMember -Function 'Parse-PolFile','Read-RegistryPolicies','Create-RegistrySettingsEntry','Create-GPRegistryPolicyFile','Append-RegistryPolicies'
