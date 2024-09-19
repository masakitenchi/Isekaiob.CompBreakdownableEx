import os
import lxml.etree as ET
import re


ascii_check = re.compile(r'[A-Za-z0-9_]+')

class csv_reader:
	def __init__(self, path):
		self.path = path
		self.file = open(path, "r", encoding="utf-8")
		self.name_dict = []
		self.data = []
		for name in self.file.readline().strip().split(","):
			self.name_dict.append(name)
	
	def read(self):
		for line in self.file.readlines():
			i = 0
			temp_dict = {}
			for data in line.strip().split(","):
				temp_dict[self.name_dict[i]] = data
				if self.name_dict[i] == 'eventMTBInTicks' and data == '':
					temp_dict[self.name_dict[i]] = 3600000
				if self.name_dict[i] == 'ticksToRepair' and data == '':
					temp_dict[self.name_dict[i]] = 1000
				i += 1
			self.data.append(temp_dict)

	def print(self):
		print(self.data)
		print(self.name_dict)

	def format(self, format: str):
		if format == "xml":
			self.to_xml()
		elif format == "json":
			self.to_json()
		else:
			print("Format not supported")
	
	def to_xml(self):
		dict_findmodByID = {}
		root = ET.Element("Patch")
		tree = ET.ElementTree(root)
		for line in self.data:
			child = ET.Element("Operation")
			child.set("Class", "PatchOperationAddModExtension")
			if ascii_check.fullmatch(line['worktable']) is None: 
				print(f'skipping {line["worktable"]}')
				continue
			ET.SubElement(child, "xpath").text = f"Defs/ThingDef[defName=\"{line['worktable']}\"]"
			value = ET.SubElement(child, "value")
			li = ET.SubElement(value, "li")
			li.set("Class", "Isekaiob.ModExtension_Breakdownable")
			ET.SubElement(li, "eventMTBInTicks").text = str(line['eventMTBInTicks'])
			ET.SubElement(li, "RepairCost").text = line['repairCost']
			ET.SubElement(li, "label").text = line['label']
			ET.SubElement(li, "ticksToRepair").text = str(line['ticksToRepair'])
			if line['ModId'] != '':
				if dict_findmodByID.get(line['ModId']) == None:
					dict_findmodByID[line['ModId']] = [child]
				else:
					dict_findmodByID[line['ModId']].append(child)
			else:
				root.append(child)
		for key, value in dict_findmodByID.items():
			findModById = ET.SubElement(root, "Operation")
			findModById.set("Class", "PatchOperation.FindModByID")
			mods = ET.SubElement(findModById, "mods")
			ET.SubElement(mods, "li").text = key
			match = ET.SubElement(findModById, "match")
			match.set("Class", "PatchOperationSequence")
			operations = ET.SubElement(match, "operations")
			for operation in value:
				operation.tag = "li"
				operations.append(operation)
		tree.write("Patches/output.xml", pretty_print=True, xml_declaration=True, encoding="utf-8")


if __name__ == "__main__":
	reader = csv_reader("./test.csv")
	reader.read()
	reader.format("xml")