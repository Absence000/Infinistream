import os, random, time, asyncio, shutil, numpy
from pathlib import Path
import openai
from datetime import datetime
from PIL import Image

scriptPath = os.getcwd()

from spacesavers import *

config = openjson("config")

openai.api_key = config["OPENAI_KEY"]

import ttsGen

index = 0
database = ""
async def writeScript():
    # Gets the index of the scene
    global index
    global database

    ind = openjson("index")
    index = int(ind["index"])
    # I thought that by separating the database + index into different json files it would be more efficient but the
    # difference is negligible

    database = openjson("database")

    # checks if it's read-only or not
    hour = datetime.now().strftime("%H")

    if index > config["MAX_INDEX_LENGTH"]:
        index = 0

    write = hour in config["HOURS_TO_GENERATE_TEXT"] or str(index) not in database

    if write:
        global driver
        print(f"Generating new script at index {index}")

        moodDescriptor = generateMoodDescriptor()

        # debugging
        if len(moodDescriptor) > 0:
            print("Mood Modifier: " + moodDescriptor)

        # writes the script
        script = ask(
            config["MAIN_PROMPT"] + moodDescriptor + config["EXTRA_INSTRUCTIONS"])

        # makes sure the script is suitable
        while not checkScript(script):
            if script is None:
                print("Script generation failed! (openAI is lame)")
            else:
                print("Script generation failed! Trying again...\nFailed attempt:\n" + script)
            script = ask(
                config["MAIN_PROMPT"] + config["EXTRA_INSTRUCTIONS"], False)

        savetxt("newScript", script)

        if str(index) in database:
            database[str(index)]["script"] = script
            if "summary" in database[str(index)]:
                database[str(index)].pop("summary")

        else:
            database[str(index)] = {"script": script}

        savejson("database", database)

        print("Script ready! Generating audio...")

        await ttsGen.setupDialogue(index)
    else:
        print(f"Getting script at index {index}")
        savetxt("newScript", database[str(index)]['script'])
        try:
            shutil.copytree(f"{config['AUDIO_REPO_PATH']}/{index}/",
                            os.path.join(os.path.abspath(os.path.join(os.getcwd(), os.pardir)), "newMP3s"),
                            dirs_exist_ok=True)
            time.sleep(5)
            print("Audio found! Copying in!")
            lines = database[str(index)]["script"].splitlines()
            subtitles = ""
            for line in lines:
                if ":" in line and "FADE " not in line[:line.index(":")] and "SCENE" not in line[:line.index(
                        ":")].upper() and "CUT TO" not in line[:line.index(":")] and len(line[line.index(":"):]) > 5:
                    subtitles += line + "\n"
            savetxt("audioInfo", "done")
            savetxt("subtitles", subtitles)
        except Exception as e:
            print(e)
            print("Audio not found! Generating new TTS:")
            await ttsGen.setupDialogue(index)
    if "summary" not in database[str(index)]:
        summary = summarize(database[str(index)]["script"])
        database[str(index)]["summary"] = summary

        savejson("database", database)

    else:
        summary = database[str(index)]["summary"]

    savetxt("prompt", f"Prompt: {summary}\nRequest your own by joining our Discord!")
    print("Script summarized!")

    # Generates the textures for the unknown people!

    # First figures out which characters it needs to make custom textures for
    charList = opentxt("characterList")
    charList = charList[0].split(" ")[:-1]
    charactersWithExistingTextures = config["CANON_CHARACTERS"]
    newCharList = []
    for character in charList:
        character = character.replace("_", " ")
        # scans for nicknames
        for names in config["NICKNAMES"].items():
            for nickname in names[1]:
                if character == nickname:
                    character = names[0]
        if character.lower() not in charactersWithExistingTextures:
            newCharList.append(character)
    if len(newCharList) > 0:
        # If they're in memory already then it just grabs from that, otherwise it generates them fresh
        if "unknownColors" not in database[str(index)]:
            print("Generating new unknown textures...")
            database[str(index)]["unknownColors"] = askAndGenerateunknownTextures(newCharList)
            with open(f"database.json", "w") as file:
                json.dump(database, file, indent=4)
        else:
            print("Grabbing unknown textures...")
            generateUnknownTextures(database[str(index)]["unknownColors"], newCharList)

    index += 1
    ind["index"] = index

    savejson("index", ind)

    print("Scene ready!")


def generateMoodDescriptor():
    queue = openjson("queue")
    if len(queue) == 0:
        descriptorDict = openjson("descriptors")
        weights = []
        indexes = []
        for descriptor in descriptorDict.items():
            weights.append(int(descriptor[1]['weight']))
            indexes.append(int(descriptor[0]))
        choice = random.choices(indexes, weights=weights)[0]
        moodDescriptor = descriptorDict[str(choice)]['name']
        # laugh track is done independently
        # laughOrNot = random.randint(0, 100)
        # if 0 < laughOrNot < config["LAUGH_TRACK_PERCENTAGE"]:
        #     moodDescriptor += " with a out-of-place laugh track"
        # else:
        #     moodDescriptor += " and don't add a laugh track"
        return moodDescriptor
    else:
        moodDescriptor = list(queue[0].items())[0][1]['modifier']
        queue.pop(0)
        savejson("queue", queue)
        return moodDescriptor


def checkScript(script):
    if script is None:
        return False
    if len(script) <= 30:
        return False
    script = script.lower()
    canonCharacters = config["CANON_CHARACTERS"]
    for line in script.splitlines():
        for character in canonCharacters:
            if line.startswith(f"{character}"):
                return True
    return False


default = [{"role": "system",
            "content": config["SYSTEM_MESSAGE"]}]
messageList = default


def ask(question, remember_previous=None):
    global messageList
    global index
    global database
    try:
        if remember_previous:
            raise Exception("resetting the bot")
        if not config["REMEMBER_PREVIOUS_SCRIPTS"]:
            messageList = default
        else:
            # fills out the message list with previous scripts if the bot was restarted
            if len(messageList) < 3:
                backtrack = 1
                while len(messageList) < 3:
                    try:
                        messageList.append({"role": "assistant", "content": database[str(index-backtrack)]["script"]})
                        backtrack += 1
                        print(f"Added scene at index {index - backtrack} to memory!")
                    except:
                        break
            pass
        messageList.append({"role": "user", "content": question})
        response = openai.chat.completions.create(
            model="gpt-3.5-turbo",
            messages=messageList
        )
        message = response.choices[0].message
        if len(messageList) > 3:
            del messageList[1]
        del messageList[len(messageList) - 1]
        messageList.append({"role": message.role, "content": message.content})
        print(message.content)
        # print("Tokens: " + message["usage"]["total_tokens"])
        return (message.content)
    except:
        time.sleep(1)
        print("resetting chatbot!")
        messageList = default
        messageList.append({"role": "user", "content": question})
        response = openai.chat.completions.create(
            model="gpt-3.5-turbo",
            messages=messageList
        )
        message = response.choices[0].message
        messageList.append({"role": message.role, "content": message.content})
        print(message.content)
        return (message.content)


def summarize(script):
    try:
        response = openai.chat.completions.create(
            model="gpt-3.5-turbo",
            messages=[{"role": "user", "content": f"Summarize this script for me very concisely: {script}"}]
        )
        message = response.choices[0].message
        print(message.content)
        return (message.content)
    except:
        print("Error!")
        return ("An error occured while getting the prompt.")



#Only call if the key "unknownCharacters" isn't in the index (or if there's no script ofc)
def askAndGenerateunknownTextures(charList):
    # parses the data
    index = 0
    prompt = "Answer each prompt with a hex code separated by a newline:\n\n"
    colorables = {"skin": "FF", "shirt": "BF", "pants": "99", "shoes": "80", "hair": "E6", "eye": "4D"}
    for char in charList:
        for item in colorables.items():
            prompt += f"Make up a color for the {item[0]} color of the character {char}.\n"
        index += 1
        prompt += "\n"

    #asks the prompt!
    ans = requestColors(prompt).split("\n")
    #parses it
    rgbs = []
    index = 0
    for answer in ans:
        if len(answer) > 0 and answer[0] == "#":
            ans[index] = answer[1:]
            try:
                rgbs.append(hextoRGB(answer))
            except:
                randomColor = generateRandomColor()
                rgbs.append(randomColor)
                print("failed")
        index += 1
    desiredLen = 6 * len(charList)
    if len(rgbs) > desiredLen:
        rgbs = rgbs[:desiredLen]
    if len(rgbs) < desiredLen:
        #appends a random color
        while len(rgbs) < desiredLen:
            rgbs.append(generateRandomColor())
    print(rgbs)
    #generates the image for each character
    index = 0
    for char in charList:
        print(char)
        im = Image.open(f"{config['ASSET_PATH']}/unknown-greyscale.png")
        data = numpy.array(im)
        for color in colorables.items():
            changeColorBand(data, color[1], rgbs[index])

            index += 1
        changeColorBand(data, "33", (255, 255, 255))
        im = Image.fromarray(data)
        try:
            im.save(f'{config['ASSET_PATH']}/tempTextures/{char.replace("_", " ")}.png')
        except:
            f"Failed with character {char}"
    return(rgbs)

def generateUnknownTextures(rgbs, charList):
    #generates the image for each character
    index = 0
    colorables = {"skin": "FF", "shirt": "BF", "pants": "99", "shoes": "80", "hair": "E6", "eye": "4D"}
    for char in charList:
        if char != "":
            im = Image.open(f'{config['ASSET_PATH']}/unknown-greyscale.png')
            data = numpy.array(im)
            for color in colorables.items():
                if len(rgbs) > index:
                    changeColorBand(data, color[1], rgbs[index])

                index += 1
            changeColorBand(data, "33", (255, 255, 255))
            im = Image.fromarray(data)
            print(char)
            im.save(f'{config['ASSET_PATH']}/tempTextures/{char.replace("_", " ")}.png')

def generateRandomColor():
    randomColor = ()
    for i in range(3):
        randomColor += (random.randint(0, 255),)
    return(randomColor)


def changeColorBand(data, band, newColor):
    for i in range(2):
        band += band
    band = hextoRGB(band)
    red, green, blue = data[:, :, 0], data[:, :, 1], data[:, :, 2]
    mask = (red == band[0]) & (green == band[1]) & (blue == band[2])
    data[:, :, :3][mask] = list(newColor)

    return data
    # returns the raw image data to save processing power

def hextoRGB(code):
    code = code.lstrip('#')
    rgb = tuple(int(code[i:i + 2], 16) for i in (0, 2, 4))
    return(rgb)

def requestColors(prompt):
    try:
        response = openai.chat.completions.create(
            model="gpt-3.5-turbo",
            messages=[{"role": "user", "content": prompt}]
        )
        message = response.choices[0].message
        print(message.content)
        return (message.content)
    except:
        print("Error!")
        return("#FFFFFF")


running = True


async def main():
    global genNew
    if os.path.exists("newScript.txt"):
        os.remove(f"newScript.txt")
    # executes once per second, ik it's dumb but it works
    while running:
        genNew = opentxt("extControl")
        shift = opentxt("shift")
        if genNew == ["run"]:
            savetxt("audioInfo", "generating")
            await writeScript()
        if shift == ["shift"]:
            print("shifting...")
            time.sleep(5)
            Shift()
        time.sleep(1)


def Shift():
    if Path(f"{scriptPath}/newScript.txt").is_file():
        os.remove(f"{scriptPath}/script.txt")
        os.rename(f"{scriptPath}/newScript.txt",
                  f"{scriptPath}/script.txt")


genNew = [""]
index = 2

loop = asyncio.get_event_loop()

loop.run_until_complete(main())