﻿function HydroCore() {
  let promiseChain = {};

  const configMeta = document.querySelector('meta[name="hydro-config"]');
  const config = configMeta ? JSON.parse(configMeta.content) : {};

function injectScript(componentElement, scriptContent) {
  const script = document.createElement('script');
  script.type = 'text/javascript';
  script.innerHTML = `
    (function($component) {
      ${scriptContent}
    })(document.getElementById("${componentElement.id}"));
  `;
  script.setAttribute('data-injected', 'true');
  componentElement.appendChild(script);
}

  function injectScriptAtSamePosition(existingScript) {
    const newScript = document.createElement('script');
    newScript.type = 'text/javascript';
    newScript.innerHTML = existingScript.innerHTML;
    newScript.setAttribute('data-injected', 'true');
    existingScript.parentNode.replaceChild(newScript, existingScript);
}


  function postProcessHeaders(headers, element) {

        //used by ExecuteJs
        const scripts = headers.get('hydro-js');
            if (scripts) {
                const scriptsArray = JSON.parse(scripts);
                scriptsArray.forEach(script => {
                    injectScript(element, script);
                });
            }
  }

    function execProperScripts(element) {
      const scripts = element.querySelectorAll('script:not([type]):not([data-injected]), script[type="text/javascript"]:not([data-injected])');
      scripts.forEach(script => {
        try {
            
            injectScriptAtSamePosition(script);

        } catch (e) {
          console.error('Error executing script:', e, script.innerHTML);
        }
      });
  }

  async function loadPageContent(url, selector, push, condition, payload) {
    const element = document.querySelector(selector);
    element.classList.add('hydro-loading');

    try {
      let headers = {
        'Hydro-Request': 'true',
        'Hydro-Boosted': 'true'
      };

      if (payload) {
        headers['Hydro-Payload'] = JSON.stringify(payload);
      }

      const response = await fetch(url, {
        method: 'GET',
        headers
      });

      if (condition && !condition()) {
        throw new Error(`Request stopped`);
      }

      if (!response.ok) {
        const eventDetail = {
          message: "Problem with loading the content",
          data: null
        }
        document.dispatchEvent(new CustomEvent(`global:UnhandledHydroError`, { detail: { data: toBase64Json(eventDetail) } }));
        throw new Error(`HTTP error! status: ${response.status}`);
      } else {
        let data = await response.text();
        
        let parser = new DOMParser();
        let doc = parser.parseFromString(data, 'text/html');

        let newContent = doc.querySelector('body');
        let insideLayout = doc.querySelector(selector);
        if (insideLayout) {
            newContent = insideLayout;
        }        
        element.innerHTML = newContent.innerHTML;       

        let newTitle = doc.querySelector('head>title');
            if (newTitle) {
            document.title = newTitle.textContent;
        }
        
        if (push) {
          history.pushState({}, '', url);
        }

        if (selector === findLocationTarget() && !window.location.hash) {
            window.scrollTo(0, 0);
        }
 
        //exec scripts loaded along with new ajax-loaded content
        execProperScripts(element);

        document.dispatchEvent(new CustomEvent('HydroLocation', {
                detail: { url, selector, push, payload }
            }));

      }
    } catch (error) {
      if (error.message === 'Request stopped') {
      } else {
        console.error('Fetch error: ', error);
        window.location.href = url;
      }
    } finally {
      element.classList.remove('hydro-loading');
    }
  }

  function enqueueHydroPromise(key, promiseFunc) {
    if (!promiseChain[key]) {
      promiseChain[key] = Promise.resolve();
    }

    let lastPromise = promiseChain[key];
    promiseChain[key] = promiseChain[key].then(() =>
      promiseFunc()
        .catch(error => {
          console.log(`Error: ${error}`);
          // throw error;
        })
    );
    return promiseChain[key];
  }

  function generateGuid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
      const r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }

  function findComponent(el) {
    const component = el.closest("[hydro]");

    if (!component) {
      return null;
    }

    return {
      id: component.getAttribute("id"),
      name: component.getAttribute("hydro-name"),
      element: component
    };
  }

  let binding = {};
  let dirty = {};

  async function hydroEvent(el, url, eventData) {
    const operationId = eventData.operationId;
    const hydroEvent = el.getAttribute("x-on-hydro-event");
    const wireEventData = JSON.parse(hydroEvent);
    await hydroRequest(el, url, { eventData: { name: wireEventData.name, data: eventData.data, subject: eventData.subject } }, 'event', wireEventData, operationId);
  }

  async function hydroBind(el, debounce) {
    if (!isElementDirty(el)) {
      return;
    }

    const component = findComponent(el);

    if (!component) {
      throw new Error('Cannot find Hydro component');
    }

    const url = `/hydro/${component.name}`;

    if (!binding[component.id]) {
      binding[component.id] = {
        formData: new FormData(),
        operationId: generateGuid()
      };
    }

    const formData = binding[component.id].formData;
    const bindAlreadyInitialized = [...formData].length !== 0;

    if (!bindAlreadyInitialized) {
      binding[component.id].operationId = generateGuid();
    }

    let propertyName = el.getAttribute('name');

    if (el.tagName === "INPUT" && el.type === 'checkbox') {
      formData.set(propertyName, el.checked);
    } else if (el.tagName === "INPUT" && el.type === 'file') {
      if (el.files.length) {
        formData.set(propertyName, el.files[0]);
      } else {
        formData.set(propertyName, new Blob(), '');
      }
    } else {
      formData.set(propertyName, el.value);
    }

    el.setAttribute("hydro-operation-id", binding[component.id].operationId);
    el.classList.add('hydro-request');

    if (bindAlreadyInitialized) {
      return Promise.resolve();
    }

    if (binding[component.id].timeout) {
      clearTimeout(binding[component.id].timeout);
    }

    
    binding[component.id].promise = new Promise(resolve => {
      binding[component.id].timeout = setTimeout(async () => {
        const requestFormData = binding[component.id].formData;
        // binding[url].formData = new FormData();
        const bindOperationId = binding[component.id].operationId;
        dirty[propertyName] = bindOperationId;
        await hydroRequest(el, url, { formData: requestFormData }, 'bind', null, bindOperationId);
        if (dirty[propertyName] === bindOperationId) {
          delete dirty[propertyName];
        }
        resolve();
      }, Math.max(debounce, 10));
    });

    return binding[component.id].promise;
  }

  async function hydroAction(el, component, action, data) {
    const url = `/hydro/${component.name}/${action.name}`;

    if (Array.from(el.attributes).some(attr => attr.name.startsWith('x-hydro-bind')) && isElementDirty(el)) {
      await hydroBind(el);
    }

    if (document.activeElement && document.activeElement !== el) {
      const elToCheck = document.activeElement;
      if (Array.from(elToCheck.attributes).some(attr => attr.name.startsWith('x-hydro-bind')) && isElementDirty(elToCheck)) {
        await hydroBind(elToCheck);
      }
    }

    const operationId = generateGuid();
    el.setAttribute("hydro-operation-id", operationId);

    await hydroRequest(el, url, { parameters: action.parameters }, 'action', data, operationId, true);
  }

  let operationStatus = {};

  async function hydroRequest(el, url, requestData, type, eventData, operationId, morphActiveElement) {
    if (!document.contains(el)) {
      return;
    }

    const component = el.closest("[hydro]");
    const componentId = component.getAttribute("id");
    const componentName = component.getAttribute("hydro-name");

    if (!component) {
      throw new Erorr('Cannot determine the closest Hydro component');
    }

    const parentComponent = findComponent(component.parentElement);

    let disableTimer;
    let classTimeout;

    if (operationId && type !== 'bind') {
      if (!operationStatus[operationId]) {
        operationStatus[operationId] = 0;

        const operationTrigger = document.querySelector(`[hydro-operation-id="${operationId}"]`);

        if (operationTrigger) {
          classTimeout = setTimeout(() => operationTrigger.classList.add('hydro-request'), 200);
          disableTimer = setTimeout(() => operationTrigger.disabled = true, 200);
        }
      }

      operationStatus[operationId]++;
    }

    if (type === 'action') {
      if (binding[componentId]?.promise) {
        await binding[componentId].promise;
      }
    }

    await enqueueHydroPromise(componentId, async () => {
      try {
        let headers = {
          'Hydro-Request': 'true'
        };

        if (config.Antiforgery) {
          headers[config.Antiforgery.HeaderName] = config.Antiforgery.Token;
        }

        let requestForm = requestData?.formData || new FormData();

        requestForm.append('__hydro_type', type);

        if (requestData.parameters) {
          requestForm.append('__hydro_parameters', JSON.stringify(requestData.parameters))
        }

        if (requestData.eventData) {
          requestForm.append('__hydro_event', JSON.stringify(requestData.eventData))
        }

        // NEW post form from eventData
        if (eventData instanceof HTMLFormElement) {
        new FormData(eventData).forEach((value, key) => {
            requestForm.append(key, value);
        });
        }

        const scripts = component.querySelectorAll('script[data-id]');
        const dataIds = Array.from(scripts).map(script => script.getAttribute('data-id')).filter(d => d !== componentId);
        requestForm.append('__hydro_componentIds', JSON.stringify([componentId, ...dataIds]));

        const scriptTag = component.querySelector(`script[data-id='${componentId}']`);
        requestForm.append('__hydro_model', scriptTag.textContent);

        if (operationId) {
          headers['Hydro-Operation-Id'] = operationId;
        }

        if (type === 'bind') {
          binding[componentId].formData = new FormData();
        }

        const response = await fetch(url, {
          method: 'POST',
          body: requestForm,
          headers: headers
        });

        if (!response.ok) {
          if (response.status === 400 && config.Antiforgery && response.headers.get("Refresh-Antiforgery-Token")) {
            const json = await response.json();
            config.Antiforgery.Token = json.token;
          } else if (response.status === 403) {
            if (type !== 'event') {
              document.dispatchEvent(new CustomEvent(`global:UnhandledHydroError`, { detail: { data: toBase64Json({ message: "Unauthorized access" }) } }));
            }
            throw new Error(`HTTP error! status: ${response.status}`);
          } else {
            const contentType = response.headers.get("content-type");
            let eventDetail = {};

            try {
              if (contentType && contentType.indexOf("application/json") !== -1) {
                const json = await response.json();
                eventDetail.message = json?.message || "Unhandled exception";
                eventDetail.data = json?.data;
              } else {
                eventDetail.message = await response.text();
              }
            } catch {
              // ignore
            }

            if (type !== 'event') {
              document.dispatchEvent(new CustomEvent(`global:UnhandledHydroError`, { detail: { data: toBase64Json(eventDetail) } }));
              document.dispatchEvent(new CustomEvent(`unhandled-hydro-error`, { detail: eventDetail }));
            }
            throw new Error(`HTTP error! status: ${response.status}`);
          }
        } else {
          const skipOutputHeader = response.headers.get('Hydro-Skip-Output');

          if (!skipOutputHeader) {
            const responseData = await response.text();
            let counter = 0;

            Alpine.morph(component, responseData, {
              updating: (from, to, childrenOnly, skip) => {
                if (counter !== 0 && to.getAttribute && to.getAttribute("hydro") !== null && from.getAttribute && from.getAttribute("hydro") !== null) {
                  if (to.getAttribute("hydro-placeholder") !== null) {
                    // hydro component placeholder, so skipping
                    skip();
                    counter++;
                    return;
                  }
                }

                if (from.getAttribute && from.getAttribute("hydro-operation-id")) {
                  if (type === 'bind' && operationId !== from.getAttribute("hydro-operation-id") && from.getAttribute("hydro") === null) {
                    skip();
                    counter++;
                    return;
                  }

                  to.setAttribute("hydro-operation-id", from.getAttribute("hydro-operation-id"));
                  to.disabled = from.disabled;
                  if (from.classList.contains('hydro-request')) {
                    to.classList.add('hydro-request');
                  }
                }

                const fieldName = from.getAttribute && from.getAttribute("name");

                if (fieldName && dirty[fieldName] && dirty[fieldName] !== operationId) {
                  skip();
                  counter++;
                  return;
                } else {
                  if (from.tagName === "INPUT" && from.type === 'checkbox') {
                    from.checked = to.checked;
                  }

                  if (from.tagName === "INPUT" && from.type === 'radio') {
                    from.checked = to.checked;
                  }

                  if (from.tagName === "INPUT" && ['text', 'number', 'date'].includes(from.type) && from.value !== to.getAttribute("value")) {
                    if (document.activeElement !== from || (morphActiveElement && from.value !== to.value)) {
                      from.value = to.value;
                    }
                  }
                }

                counter++;
              }
            });

            setTimeout(() => {

                document.dispatchEvent(new CustomEvent('HydroComponentUpdate', {
                    detail: { componentId, componentName, url, type }
                    }));

                //inject all non-hydro scripts ajax-loaded along with new content
                execProperScripts(component);
           
                postProcessHeaders(response.headers, component);

            }, 20);

          }

          const locationHeader = response.headers.get('Hydro-Location');
          if (locationHeader) {
            let locationData = JSON.parse(locationHeader);

            await loadPageContent(locationData.path, locationData.target || findLocationTarget(), true, null, locationData.payload);
          }

          const redirectHeader = response.headers.get('Hydro-Redirect');
          if (redirectHeader) {
            window.location.href = redirectHeader;

            document.dispatchEvent(new CustomEvent('HydroRedirect', {
                detail: { redirectHeader }
            }));
          }

          setTimeout(() => { // make sure the event handlers got registered
            const triggerHeader = response.headers.get('Hydro-Trigger');
            if (triggerHeader) {
              const triggers = JSON.parse(triggerHeader);
              triggers.forEach(trigger => {
                if (trigger.scope === 'parent' && !parentComponent) {
                  return;
                }

                const eventScope = trigger.scope === 'parent' ? parentComponent.id : 'global';
                const eventName = `${eventScope}:${trigger.name}`;
                const eventData = {
                  detail: {
                    data: trigger.data,
                    subject: trigger.subject,
                    operationId: trigger.operationId
                  }
                };

                document.dispatchEvent(new CustomEvent(eventName, eventData));

                if (trigger.subject) {
                  const subjectEventName = `${eventScope}:${trigger.name}:${trigger.subject}`;
                  document.dispatchEvent(new CustomEvent(subjectEventName, eventData));
                }
              });
            }
          });
        }
      } catch (error) {
        document.dispatchEvent(new CustomEvent(`unhandled-hydro-error`, { detail: { name: error.name, message: error.message } }));
      } finally {
        setTimeout(() => {
          clearTimeout(classTimeout);
          clearTimeout(disableTimer);

          if (operationId) {
            const operationTrigger = document.querySelectorAll(`[hydro-operation-id="${operationId}"]`);
            operationStatus[operationId]--;

            if (operationTrigger.length && (operationStatus[operationId] <= 0 || type === 'bind')) {
              operationTrigger.forEach(trigger => {
                trigger.disabled = false;
                trigger.classList.remove('hydro-request');
                trigger.removeAttribute('hydro-operation-id');
              })
            }
          }
        }, 20); // make sure it's delayed more than 0 and less than 50
      }
    });
  }

  function isElementDirty(element) {
    const type = element.type;
    if (['checkbox', 'radio'].includes(type)) {
      if (element.checked !== element.defaultChecked) {
        return true;
      }
    } else if (['hidden', 'password', 'text', 'textarea', 'number', 'date'].includes(type)) {
      if (element.value !== element.defaultValue) {
        return true;
      }
    } else if (['file'].includes(type)) {
      return true;
    } else if (["select-one", "select-multiple"].includes(type)) {
      for (let j = 0; j < element.options.length; j++) {
        if (element.options[j].selected !== element.options[j].defaultSelected) {
          return true;
        }
      }
    }
  }

  function toBase64Json(value) {
    return value !== null && value !== undefined
      ? btoa(String.fromCodePoint(...new TextEncoder().encode(JSON.stringify(value))))
      : null;
  }

  function findLocationTarget() {
    let hydro = document.querySelector('#hydro-location');
    if (hydro) {
        return `#hydro-location`;        
    }
    return `body`;
  }

  window.addEventListener('popstate', async function () {
    await loadPageContent(window.location.href, findLocationTarget(), false);
  });

  return {
    hydroEvent,
    hydroBind,
    hydroAction,
    injectScript,
    loadPageContent,
    findComponent,
    generateGuid,
    config
  };
}

window.Hydro = new HydroCore();

document.addEventListener('alpine:init', () => {
  Alpine.directive('hydro-dispatch', (el, { expression }, { effect, cleanup }) => {
    effect(() => {
      if (!document.contains(el)) {
        return;
      }

      const component = window.Hydro.findComponent(el);

      if (!component) {
        throw new Error("Cannot find Hydro component");
      }

      const eventName = el.getAttribute('hydro-event') || 'click';

      const parentComponent = window.Hydro.findComponent(component.element.parentElement);

      const trigger = JSON.parse(expression);

      if (trigger.scope === 'parent' && !parentComponent) {
        return;
      }

      const scope = trigger.scope === 'parent' ? parentComponent.id : 'global';

      const eventHandler = async (event) => {
        if (el.disabled) {
          return;
        }

        event.preventDefault();

        const operationId = window.Hydro.generateGuid();
        el.setAttribute("hydro-operation-id", operationId);

        const eventName = `${scope}:${trigger.name}`;
        const eventData = {
          detail: {
            data: trigger.data,
            operationId
          }
        };

        document.dispatchEvent(new CustomEvent(eventName, eventData));
      };
      el.addEventListener(eventName, eventHandler);
      cleanup(() => {
        el.removeEventListener(eventName, eventHandler);
      });
    });
  });

  Alpine.directive('hydro-bind', (el, { value, modifiers }, { effect, cleanup }) => {
    effect(() => {
      const event = value;

      const debounce = parseInt(((modifiers[0] === 'debounce' && (modifiers[1] || '250ms')) || '0ms').replace('ms', ''));

      const eventHandler = async (event) => {
        if (event === 'submit' || event === 'click') {
          event.preventDefault();
        }

        const target = event.currentTarget;
        await window.Hydro.hydroBind(target, debounce);
      };

      el.addEventListener(event, eventHandler);
      cleanup(() => {
        el.removeEventListener(event, eventHandler);
      });
    });
  }).before('on');

  Alpine.directive('hydro-polling', Alpine.skipDuringClone((el, { value, expression, modifiers }, { cleanup }) => {
    let isQueued = false;
    let interval;
    const component = window.Hydro.findComponent(el);
    const time = parseInt(modifiers[0].replace('ms', ''));

    const setupInterval = () => {
      interval = setInterval(async () => {
        if (document.hidden) {
          isQueued = true;
          clearInterval(interval);
          return;
        }

        await window.Hydro.hydroAction(el, component, { name: expression });
      }, time);
    }

    const handleVisibilityChange = async () => {
      if (!document.hidden && isQueued) {
        isQueued = false;
        await window.Hydro.hydroAction(el, component, { name: expression });
        setupInterval();
      }
    }

    document.addEventListener('visibilitychange', handleVisibilityChange);
    setupInterval();

    cleanup(() => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      clearInterval(interval);
    });

  }));

  Alpine.directive('on-hydro-event', (el, { expression }, { effect, cleanup }) => {
    effect(() => {
      const component = window.Hydro.findComponent(el);

      if (!component) {
        throw new Error("Cannot find Hydro component");
      }

      const data = JSON.parse(expression);
      const globalEventName = data.subject ? `global:${data.name}:${data.subject}` : `global:${data.name}`;
      const localEventName = data.subject ? `${component.id}:${data.name}:${data.subject}` : `${component.id}:${data.name}`;
      const eventPath = data.path;

      const eventHandler = async (event) => {
        await window.Hydro.hydroEvent(el, eventPath, event.detail);
      }

      document.addEventListener(globalEventName, eventHandler);
      document.addEventListener(localEventName, eventHandler);

      cleanup(() => {
        document.removeEventListener(globalEventName, eventHandler);
        document.removeEventListener(localEventName, eventHandler);
      });
    });
  });

  let currentBoostUrl;

  Alpine.directive('hydro-link', (el, { expression }, { effect, cleanup }) => {
    effect(() => {
      const handleClick = async (event) => {
        event.preventDefault();
        const link = event.currentTarget;
        const url = link.getAttribute('href');
        currentBoostUrl = url;
        const classTimeout = setTimeout(() => link.classList.add('hydro-request'), 200);
        try {
          await window.Hydro.loadPageContent(url, findLocationTarget(), true, () => currentBoostUrl === url);
        } finally {
          clearTimeout(classTimeout);
          link.classList.remove('hydro-request')
        }
      };

      const links = [el, ...el.querySelectorAll('a')].filter(el => el.tagName === 'A');
      links.forEach((link) => {
        link.addEventListener('click', handleClick);
      });

      cleanup(() => {
        links.forEach((link) => {
          link.removeEventListener('click', handleClick);
        });
      });
    });
  });

  Alpine.directive('hydro-focus', (el, { expression }, { effect }) => {
    effect(() => {
      el.querySelector(expression || 'input').focus();
    });
  });

  Alpine.data('hydro', () => {
    let debounceArray = {};

    return ({
      $component: null,
      init() {
        this.$component = window.Hydro.findComponent(this.$el);

        let component = this.$el;
        const hydroScripts = component.querySelectorAll('script[type="text/hydro"][hydro-js="true"]');
        hydroScripts.forEach(script => {
            try {
                    window.Hydro.injectScript(component, script.innerHTML);
                    script.parentNode.removeChild(script);
            } catch (e) {
                console.error('Error executing hydro script:', e, script.innerHTML);
            }
        });

        document.dispatchEvent(new CustomEvent('HydroComponentInit', {
            detail: { component: this.$component }
        }));
      },
      async invoke(e, action) {
        if (["click", "submit"].includes(e.type) && ['A', 'BUTTON', 'FORM'].includes(this.$el.tagName)) {
          e.preventDefault();
        }
        await window.Hydro.hydroAction(this.$el, this.$component, action);
      },
      async postForm(e, action) {
        if (["click", "submit"].includes(e.type) && ['A', 'BUTTON', 'FORM'].includes(this.$el.tagName)) {
          e.preventDefault();
        }
        await window.Hydro.hydroAction(this.$el, this.$component, action, e.target);
      },
      async bind(debounce) {
        let element = this.$el;

        clearTimeout(debounceArray[element]);

        debounceArray[element] = setTimeout(async () => {
          await window.Hydro.hydroBind(element);
          delete debounceArray[element];
        }, debounce || 0);
      }
    });
  })
});
