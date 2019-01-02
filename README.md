# Grouper2
C# rewrite of Grouper - work in progress, semi-functional at best.

## TODO

Parse and assess other inf sections properly:
 - System Access
 - Kerberos Policy
 - Event Audit
 - Registry Values
 - Scheduled tasks
 - Registry Keys
 - Group Membership
 - Service General Setting

Assess more GPP types properly:
  - Reg Settings
  - Drives
  - Environment Variables
  - NT Services
  - Network Options
  - Folders
  - Network Shares
  - INI Files
  - Scheduled Tasks (currently in progress)

Expand use of 'interest levels' and maybe break the definition of interest levels into a config file
Grep scripts for creds.
Grep arguments/cmd line param strings for substrings indicating possible credentials
Enumerate File permissions for referenced files.
Figure out a cleaner way of checking if a path is writable that doesn't risk leaving droppings if a non-existent file is checked in a path that we can write to.
Parse Registry.pol
Parse Machine\Applications\*.AAS (assigned applications?
figure out what happened to MSI files?
nicen up the output, maybe some colours?
