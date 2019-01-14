# Grouper2
C# rewrite of Grouper - VERY work in progress, pre-pre-pre-alpha, semi-functional at best, etc.

I NON-IRONICALLY EAGERLY AWAIT YOUR PULL REQUEST, SEE ISSUES PAGE FOR HOW YOU CAN HELP!

## What does it for and what does it do?

Grouper2 is a tool for pentesters to help find security-related misconfigurations in Active Directory Group Policy. 

It might also be useful for other people doing other stuff, but it is explicitly NOT meant to be an audit tool. If you want to check your policy configs against some particular standard, you probably want Microsoft's Security and Compliance Toolkit, not Grouper or Grouper2.

## How is it different from Grouper?

Where Grouper required you to:

a) have GPMC/RSAT/whatever installed on a domain-joined computer

b) generate an xml report with the Get-GPOReport PowerShell cmdlet

c) feed the report to Grouper

d) a bunch of gibberish falls out and hopefully you understand what it means enough to 

Grouper2 does like Mr Ed suggests and goes straight to the source, i.e. SYSVOL. This means you don't have the horrible dependency on Get-GPOReport (hooray!) but it also means that it has to do a bunch of parsing of different file formats and so on (booo!).

If you want, you can also aim Grouper2 at an offline copy of the SYSVOL directory structure.

Also, it's written in C# instead of PowerShell.

## How does it work and what's implemented so far?

At a high level Grouper2 does like this:

1. Parses args and decides if it's in "Online" or "Offline" mode. This determines if it tries to 'follow up' on stuff it finds that might be interesting, by checking file permissions, resolving SIDs to display names, etc. One optional argument is 'interest level' which tells G2 how aggressive to be in only returning stuff that is 100% vulnerable as opposed to stuff that is maybe just kind of interesting.
2. If it's in online mode it hits AD via LDAP for a bunch of information about the GPOs in the domain, like display names and DACLs and so on.
3. It figures out where SYSVOL is, gets a list of GPO directories and starts processing them.
4. For each type of file in which policy settings can be found, (inf, xml, ini so far) it finds the files and mangles the contents into a JObject using Newtonsoft Json.NET.
5. Within each file type it takes each parsed setting type (e.g. Scheduled tasks, registry settings, users and groups, password policies etc) and feeds it to an appropriate method to 'assess' the parsed data.
6. At this stage a bunch of the methods literally do nothing but directly regurgitate the JObject that they were passed, but the ones that I've written follow a rough pattern:
- Set up a JObject for findings to go into.
- Iterate over the settings within the thing
- Set a base interest level for the setting, based on how likely I am to care about it. For example, changes to users and groups and privileges are kind of interesting no matter what the details, changes to audit logging aren't so interesting.
- Decide somehow if the setting is 'vulnerable' or 'interesting'. This might mean the simple presence or absence of some values (Group Policy Preferences Passwords) or it might involve seeing if a given file or directory exists and/or is writable in the current user's context.
- A bunch of these decisions are based on data I'm keeping in a JSON file lovingly referred to as "JankyDB".
- Based on the above, assign an 'interest level' to the finding.
- If the interest level of the finding is greater than or equal to the interest level argument set by the user, add the finding to the output JObject.
- Keep going til we run out of settings
- Return the JObject full of findings that met the criteria.
7. It does all of the above for both 'User' policy and 'Machine' policy for each Group Policy Object.
8. As each GPO is assessed, the findings from it are put into a 'final' output JObject.
9. The output JObject is written straight to the console.

## What remains to be done?

Heaps. Have a look in the Issues for the repo and just start chewing I guess.
If you want to discuss via Slack you can ping me (l0ss) on the BloodHound Slack, joinable at https://bloodhoundgang.herokuapp.com/.
