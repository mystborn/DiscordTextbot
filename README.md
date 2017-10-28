# DiscordTextbot
A discord bot written in c# using Discord.Net that allows users to send and receive text messages via discord.

## Getting Started
To use the bot, first you need to create a bot user from the discord website. Then on the first time you run the program you'll be asked for the following information
* token - The bot token you received when creating the bot account.
* channel id - The id of the discord channel you'd like to use as your texting channel. This channel will be the only one that the bot will listen for commands on.
* email address - The email adress you wish to associate with this program.
* email password - The password to that email address. Note all personal information is confidential and stored exclusivley on the users computer.
* email provider - Which email service you use. This defaults to gmail, however you can customize it. You can use any email server as long as they have support for imap and smtp.

## How To Use
The bot only uses 4 commands: 
* /add \[contact name\]|\[phone number as email address\]
* /remove \[contact name\]
* /list
* /send \[contact name or phone number\] \[message\]

### add
* Description - Adds a number into your address book.
* Usage - /add their name|their_number@carrier_email.com
* their name - The name you'd like them to have when sending or receiving messages.
* | - shift + forward slash (\)
* their_number - their phone number plus their carriers email extension (see below for list of carrier extensions)

### remove
* Description - Removes a number from your address book.
* Usage - /remove contact name

### list
* Description - Lists all of your contacts.
* Usage - /list

### send
* Description - Sends a message to a number or contact.
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
* /add_contact my friend | \0000000000@mms.att.net
* /send friend Hey friend, it's been awhile since we've talked :)

## Limitations
* Most carriers have a 140 character limit to messages sent.
* Some carriers can receive pictures, others cannot.
* Some people can send pictures, others cannot.
