# DiscordTextbot
A discord bot written in c# using Discord.Net that allows users to send and receive text messages via discord.

## Getting Started
If you want to use the bot out of the box, go ahead and download it here: <link>https://www.dropbox.com/sh/67fhh9d7fy8okdw/AAA2lNpEcCmnf5U6yMGZnUiqa?dl=0</link>

To use the bot, first you need to create a bot user from the discord website and get it's token. Then, run the program once, go to the file specified in the command prompt and fill out the information. (This will likely be found here: C:\Users\Your_Name\AppData\Roaming\discord\TextbotData\TextbotSettings.xml).

After restarting the program, it should start running.

### Config File
* Token - The bot token you received when creating the bot account
* GmailUsername - Messages have to go through a gmail acoount currently. This must be the desired email's username
* GmailPassword - Desired email's password
* EmailDisplayName - May affect how some carriers see your message. Included just in case. Set this to your name.
* DiscordChannelId - The channel that the bot will listen on for commands. (I'd go ahead and make a channel and set it so only you and your bot can see it. Use that as your texting channel

## How To Use
The bot only uses 4 commands: 
* /add_contact
* /remove_contact
* /list_contacts
* /send

### add_contact
* Usage - /add_contact their_name|their_number
* The | symbol is shift + forward slash (\)
* their_number is there phone number plus their carriers email extension (see below for list of carrier extensions)
* Currently their_name cannot have spaces.

### remove_contact
* Usage - /remove_contact contact_name

### list_contacts
* Usage - /list_contacts

### send
* Usage - /send contact_or_carrier_email this is where the message goes.
* The contact can be one that you've created or just a phone number with their carriers email address

## Carrier Extensions
* Alltel - @sms.alltelwireless.com
* At&T - @mms.att.net
* Boost Mobile - @sms.myboostmobile.com
* Cricket - @mms.mycricket.com
* Sprint - @messaging.sprintpcs.com (*untested*)
* T Mobile - @tmomail.net (*untested*)
* Verizon - @vtext.com
* Virgin Mobile - @vmobl.com

These may or may not work outside of America.

## Example
* /add_contact friend|0000000000@mms.att.net
* /send friend Hey friend, it's been awhile since we've talked :)

## Limitations
* Most carriers have a 140 character limit to messages sent.
* Some carriers can receive pictures, others cannot.
* Some people can send pictures, others cannot.
