import json

def openjson(filename):
    with open(f"{filename}.json") as file:
        return json.load(file)

def savejson(filename, data):
    with open(f"{filename}.json", "w") as file:
        json.dump(data, file, indent=4)

def opentxt(filename, read=None):
    with open(f"{filename}.txt") as file:
        if read:
            return file.read()
        else:
            return file.readlines()

def savetxt(filename, data):
    with open(f"{filename}.txt", "w") as file:
        file.write(data)