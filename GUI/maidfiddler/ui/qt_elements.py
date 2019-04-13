from PyQt5.QtCore import Qt
from PyQt5.QtWidgets import QWidget, QHBoxLayout

MIN_MAX_DICT = {
    "System.SByte": (-128, 127),
    "System.Byte": (0, 255),
    "System.Int16": (-2**15, 2**15-1),
    "System.UInt16": (0, 2**16-1),
    "System.Int32": (-2**31, 2**31-1),
    "System.UInt32": (0, 2**32-1),
    "System.Int64": (-2**63, 2**63-1),
    "System.UInt64": (0, 2**64-1)
}

FLOAT_TYPES = set([
    "System.Single",
    "System.Double",
    "System.Decimal"
])


class UiElement(object):
    def __init__(self, qt_element):
        self.qt_element = qt_element

    def value(self):
        raise NotImplementedError()

    def set_value(self, val):
        raise NotImplementedError()

    def connect(self, edit_func):
        pass


class TextElement(UiElement):
    def value(self):
        return self.qt_element.text()

    def set_value(self, val):
        self.qt_element.setText(val)

    def connect(self, edit_func):
        self.qt_element.editingFinished.connect(edit_func)


class PlainTextElement(UiElement):
    def value(self):
        return self.qt_element.plainText()

    def set_value(self, val):
        self.qt_element.setPlainText(val)

    def connect(self, edit_func):
        self.qt_element.editingFinished.connect(edit_func)


class NumberElement(UiElement):
    def __init__(self, qt_element, minVal=-2**31, maxVal=2**31-1, type=None):
        UiElement.__init__(self, qt_element)

        if type is not None and type in MIN_MAX_DICT:
            minVal, maxVal = MIN_MAX_DICT[type]

        self.qt_element.setMaximum(maxVal)
        self.qt_element.setMinimum(minVal)

    def value(self):
        return self.qt_element.value()

    def set_value(self, val):
        self.qt_element.blockSignals(True)
        self.qt_element.setValue(val)
        self.qt_element.blockSignals(False)

    def connect(self, edit_func):
        self.qt_element.valueChanged.connect(edit_func)


class ComboElement(UiElement):
    def __init__(self, qt_element):
        UiElement.__init__(self, qt_element)
        self.value_to_index_map = {}

    def index_map(self):
        return self.value_to_index_map

    def value(self):
        return int(self.qt_element.currentData())

    def set_value(self, val):
        self.qt_element.blockSignals(True)
        self.qt_element.setCurrentIndex(self.value_to_index_map[val])
        self.qt_element.blockSignals(False)


class CheckboxElement(UiElement):
    def __init__(self, qt_element):
        self.checkbox = qt_element
        widget = QWidget()
        hbox = QHBoxLayout(widget)
        hbox.addWidget(qt_element)
        hbox.setAlignment(Qt.AlignCenter)
        hbox.setContentsMargins(0, 0, 0, 0)
        widget.setLayout(hbox)
        self.qt_element = widget

    def value(self):
        return self.checkbox.checkState() == Qt.Checked

    def set_value(self, val):
        self.checkbox.blockSignals(True)
        self.checkbox.setCheckState(Qt.Checked if val else Qt.Unchecked)
        self.checkbox.blockSignals(False)

    def connect(self, edit_func):
        self.checkbox.stateChanged.connect(edit_func)
