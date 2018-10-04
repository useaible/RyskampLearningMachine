import os.path

class SaveToCSV():
    """write to csv"""

    def __init__(self, fileName):
        self.FileName = None
        self.FileObject = None
        if fileName != None:
            self.FileName = fileName
        else:
            self.FileName = "TFData"

        i = 1
        while os.path.exists(self.FileName + "_" + str(i) + ".csv"):
            i += 1
            if not os.path.exists(self.FileName + "_" + str(i) + ".csv"):
                break

        self.FileObject = open(self.FileName + "_" + str(i) + ".csv","w")

    def WriteLine(self, line):
        self.FileObject.write(line + "\n")

    def WriteList(self, list, headers):
        self.FileObject.write(headers + "\n")
        i = 0
        for x in range(0, list.Count):
            self.FileObject.write(str(i + 1) + "," + str(list[i]) + "\n")
            i += 1


