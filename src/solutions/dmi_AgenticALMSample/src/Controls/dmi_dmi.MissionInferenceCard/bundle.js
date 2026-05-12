/*
 * ATTENTION: The "eval" devtool has been used (maybe by default in mode: "development").
 * This devtool is neither made for production nor for readable output files.
 * It uses "eval()" calls to create a separate source file in the browser devtools.
 * If you are trying to read the output file, select a different devtool (https://webpack.js.org/configuration/devtool/)
 * or disable the default devtool with "devtool: false".
 * If you are looking for production-ready output files, see mode: "production" (https://webpack.js.org/configuration/mode/).
 */
var pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad;
/******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/******/ 	var __webpack_modules__ = ({

/***/ "./MissionInferenceCard/index.ts"
/*!***************************************!*\
  !*** ./MissionInferenceCard/index.ts ***!
  \***************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

eval("{__webpack_require__.r(__webpack_exports__);\n/* harmony export */ __webpack_require__.d(__webpack_exports__, {\n/* harmony export */   MissionInferenceCard: () => (/* binding */ MissionInferenceCard)\n/* harmony export */ });\nvar PRIORITY_HIGH = 100000000;\nvar PRIORITY_MEDIUM = 100000001;\nvar PRIORITY_LOW = 100000002;\nclass MissionInferenceCard {\n  constructor() {\n    // Empty\n  }\n  init(context, notifyOutputChanged, state, container) {\n    this._container = container;\n    this._card = document.createElement(\"div\");\n    this._card.className = \"mic-card\";\n    this._container.appendChild(this._card);\n  }\n  updateView(context) {\n    var _a, _b, _c;\n    var intent = (_a = context.parameters.intent.raw) !== null && _a !== void 0 ? _a : \"\";\n    var priorityValue = (_b = context.parameters.priority.raw) !== null && _b !== void 0 ? _b : null;\n    var actions = (_c = context.parameters.actions.raw) !== null && _c !== void 0 ? _c : \"\";\n    this._card.innerHTML = \"\";\n    if (!intent && !actions) {\n      this._card.innerHTML = \"<div class=\\\"mic-empty\\\">Awaiting signal analysis\\u2026</div>\";\n      return;\n    }\n    // Priority badge\n    var _this$_resolvePriorit = this._resolvePriority(priorityValue),\n      label = _this$_resolvePriorit.label,\n      cssClass = _this$_resolvePriorit.cssClass;\n    var badge = document.createElement(\"div\");\n    badge.className = \"mic-priority-badge mic-priority-\".concat(cssClass);\n    badge.textContent = \"\".concat(label, \" Priority\");\n    this._card.appendChild(badge);\n    // Header\n    var header = document.createElement(\"div\");\n    header.className = \"mic-header\";\n    header.textContent = \"FIRST CONTACT ANALYSIS\";\n    this._card.appendChild(header);\n    // Intent\n    if (intent) {\n      var intentRow = document.createElement(\"div\");\n      intentRow.className = \"mic-row\";\n      intentRow.innerHTML = \"<span class=\\\"mic-label\\\">SIGNAL INTENT</span><span class=\\\"mic-value\\\">\".concat(this._escape(intent), \"</span>\");\n      this._card.appendChild(intentRow);\n    }\n    // Actions\n    if (actions) {\n      var actionsSection = document.createElement(\"div\");\n      actionsSection.className = \"mic-actions-section\";\n      actionsSection.innerHTML = \"<div class=\\\"mic-label\\\">RECOMMENDED ACTIONS</div>\";\n      var actionList = document.createElement(\"ul\");\n      actionList.className = \"mic-action-list\";\n      var actionItems = actions.split(\";\").map(a => a.trim()).filter(a => a.length > 0);\n      actionItems.forEach(action => {\n        var li = document.createElement(\"li\");\n        li.textContent = action;\n        actionList.appendChild(li);\n      });\n      actionsSection.appendChild(actionList);\n      this._card.appendChild(actionsSection);\n    }\n  }\n  getOutputs() {\n    return {};\n  }\n  destroy() {\n    // Cleanup\n  }\n  _resolvePriority(value) {\n    switch (value) {\n      case PRIORITY_HIGH:\n        return {\n          label: \"HIGH\",\n          cssClass: \"high\"\n        };\n      case PRIORITY_MEDIUM:\n        return {\n          label: \"MEDIUM\",\n          cssClass: \"medium\"\n        };\n      case PRIORITY_LOW:\n        return {\n          label: \"LOW\",\n          cssClass: \"low\"\n        };\n      default:\n        return {\n          label: \"UNKNOWN\",\n          cssClass: \"unknown\"\n        };\n    }\n  }\n  _escape(text) {\n    return text.replace(/&/g, \"&amp;\").replace(/</g, \"&lt;\").replace(/>/g, \"&gt;\").replace(/\"/g, \"&quot;\");\n  }\n}\n\n//# sourceURL=webpack://pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad/./MissionInferenceCard/index.ts?\n}");

/***/ }

/******/ 	});
/************************************************************************/
/******/ 	// The require scope
/******/ 	var __webpack_require__ = {};
/******/ 	
/************************************************************************/
/******/ 	/* webpack/runtime/define property getters */
/******/ 	(() => {
/******/ 		// define getter functions for harmony exports
/******/ 		__webpack_require__.d = (exports, definition) => {
/******/ 			for(var key in definition) {
/******/ 				if(__webpack_require__.o(definition, key) && !__webpack_require__.o(exports, key)) {
/******/ 					Object.defineProperty(exports, key, { enumerable: true, get: definition[key] });
/******/ 				}
/******/ 			}
/******/ 		};
/******/ 	})();
/******/ 	
/******/ 	/* webpack/runtime/hasOwnProperty shorthand */
/******/ 	(() => {
/******/ 		__webpack_require__.o = (obj, prop) => (Object.prototype.hasOwnProperty.call(obj, prop))
/******/ 	})();
/******/ 	
/******/ 	/* webpack/runtime/make namespace object */
/******/ 	(() => {
/******/ 		// define __esModule on exports
/******/ 		__webpack_require__.r = (exports) => {
/******/ 			if(typeof Symbol !== 'undefined' && Symbol.toStringTag) {
/******/ 				Object.defineProperty(exports, Symbol.toStringTag, { value: 'Module' });
/******/ 			}
/******/ 			Object.defineProperty(exports, '__esModule', { value: true });
/******/ 		};
/******/ 	})();
/******/ 	
/************************************************************************/
/******/ 	
/******/ 	// startup
/******/ 	// Load entry module and return exports
/******/ 	// This entry module can't be inlined because the eval devtool is used.
/******/ 	var __webpack_exports__ = {};
/******/ 	__webpack_modules__["./MissionInferenceCard/index.ts"](0,__webpack_exports__,__webpack_require__);
/******/ 	pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad = __webpack_exports__;
/******/ 	
/******/ })()
;
if (window.ComponentFramework && window.ComponentFramework.registerControl) {
	ComponentFramework.registerControl('dmi.MissionInferenceCard', pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad.MissionInferenceCard);
} else {
	var dmi = dmi || {};
	dmi.MissionInferenceCard = pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad.MissionInferenceCard;
	pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad = undefined;
}