<script setup lang="ts">
import { useWizardStore, STEP_TITLES } from '@/stores/wizard'
import { useRouter } from 'vue-router'
import { markRaw, computed } from 'vue'
import Step1SourceApi from '@/components/wizard/Step1SourceApi.vue'
import Step2Endpoint from '@/components/wizard/Step2Endpoint.vue'
import Step3Mapping from '@/components/wizard/Step3Mapping.vue'
import Step4Aggregation from '@/components/wizard/Step4Aggregation.vue'
import Step5Output from '@/components/wizard/Step5Output.vue'
import Step6TestActivate from '@/components/wizard/Step6TestActivate.vue'

const wizard = useWizardStore()
const router = useRouter()

const stepComponents = [
  markRaw(Step1SourceApi),
  markRaw(Step2Endpoint),
  markRaw(Step3Mapping),
  markRaw(Step4Aggregation),
  markRaw(Step5Output),
  markRaw(Step6TestActivate),
]

const activeComponent = computed(() => stepComponents[wizard.currentStep - 1])

function handleCancel() {
  wizard.resetWizard()
  router.push('/dashboard')
}

function handleNext() {
  if (wizard.currentStepValid && !wizard.isLastStep) {
    wizard.nextStep()
  }
}

function handleBack() {
  if (!wizard.isFirstStep) {
    wizard.prevStep()
  }
}
</script>

<template>
  <div class="max-w-4xl mx-auto">
    <h2 class="text-2xl font-bold text-gray-900 mb-6">New Connection</h2>

    <!-- Step Progress Bar -->
    <nav aria-label="Wizard progress" class="mb-8">
      <ol class="flex items-center w-full">
        <li
          v-for="(title, idx) in STEP_TITLES"
          :key="idx"
          class="flex items-center"
          :class="idx < STEP_TITLES.length - 1 ? 'flex-1' : ''"
        >
          <div class="flex flex-col items-center text-center min-w-0">
            <span
              class="flex items-center justify-center w-8 h-8 rounded-full text-sm font-semibold shrink-0"
              :class="{
                'bg-indigo-600 text-white': wizard.currentStep === idx + 1,
                'bg-indigo-100 text-indigo-700': wizard.currentStep > idx + 1,
                'bg-gray-200 text-gray-500': wizard.currentStep < idx + 1,
              }"
              :data-testid="`step-indicator-${idx + 1}`"
            >
              <template v-if="wizard.currentStep > idx + 1">
                <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                  <path
                    fill-rule="evenodd"
                    d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                    clip-rule="evenodd"
                  />
                </svg>
              </template>
              <template v-else>{{ idx + 1 }}</template>
            </span>
            <span
              class="mt-1 text-xs font-medium whitespace-nowrap"
              :class="{
                'text-indigo-700': wizard.currentStep >= idx + 1,
                'text-gray-400': wizard.currentStep < idx + 1,
              }"
            >
              {{ title }}
            </span>
          </div>
          <div
            v-if="idx < STEP_TITLES.length - 1"
            class="flex-1 h-0.5 mx-2 mt-[-1rem]"
            :class="wizard.currentStep > idx + 1 ? 'bg-indigo-400' : 'bg-gray-200'"
          />
        </li>
      </ol>
    </nav>

    <!-- Step Content -->
    <div class="bg-white shadow-sm border border-gray-200 rounded-lg p-6 mb-6">
      <component :is="activeComponent" />
    </div>

    <!-- Navigation Buttons -->
    <div class="flex items-center justify-between">
      <button
        type="button"
        class="rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
        @click="handleCancel"
      >
        Cancel
      </button>
      <div class="flex gap-3">
        <button
          v-if="!wizard.isFirstStep"
          type="button"
          class="rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          data-testid="back-button"
          @click="handleBack"
        >
          Back
        </button>
        <button
          v-if="!wizard.isLastStep"
          type="button"
          class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
          :disabled="!wizard.currentStepValid"
          data-testid="next-button"
          @click="handleNext"
        >
          Next
        </button>
      </div>
    </div>
  </div>
</template>
