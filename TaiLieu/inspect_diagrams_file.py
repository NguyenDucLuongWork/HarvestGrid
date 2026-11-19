import xml.etree.ElementTree as ET

tree = ET.parse(r'c:\_Luong\Studying\GameDev\HarvestGrid_Project\TaiLieu\Diagrams')
root = tree.getroot()
print('Root tag:', root.tag, root.attrib)
for i, diag in enumerate(root.findall('diagram')):
    print(f'Diagram {i}: name="{diag.get("name")}", id="{diag.get("id")}"')
